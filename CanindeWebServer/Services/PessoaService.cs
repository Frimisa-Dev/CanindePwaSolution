#nullable enable
using CanindePwaShared.Enums;
using CanindePwaShared.Models.Dto.TableDto;
using CanindePwaShared.Models.Tbl;
using CanindeWebServer.Data;
using CanindeWebServer.Services.Utils;
using CanindeWebServer.Utils;
using Microsoft.EntityFrameworkCore;

namespace CanindeWebServer.Services
{
    /// <summary>
    /// Cadastro e consulta de pessoas — estrutura do banco verificada em 2026-11-19.
    ///
    /// MODELO
    ///  - tblpessoa guarda o comum; tblpessoafisica OU tblpessoajuridica guardam o específico.
    ///    CodigoPessoaTipo: 1 = física, 2 = jurídica.
    ///  - O e-mail fica em tblpessoaemail; tblpessoa.ID_PessoaEmailCadastro aponta para ele.
    ///  - O PERFIL é a existência de uma linha em tblcdpessoa&lt;perfil&gt;. Sem linha = pessoa sem perfil.
    ///  - Regra de combinação (validada AQUI; o banco não impõe): nenhum, um, ou Embarcador + Agenciador.
    ///
    /// REGRAS DE SEGURANÇA DE DADOS (banco em produção)
    ///  - Nunca altera estrutura. Só INSERT/UPDATE de dados, em transação.
    ///  - Perfis e e-mail são gravados/consultados por SQL parametrizado sobre colunas VERIFICADAS,
    ///    para não depender de detalhes das classes de entidade.
    ///  - Perfis Embarcador, Agenciador, Corretor e ParceiroFrete exigem ID_Usuario (NOT NULL, FK para
    ///    tblusuario). Quem cadastra informa o ID_Usuario (usuário da tblusuario ao qual a pessoa fica
    ///    ligada); a API confere que ele existe. Sem ID_Usuario -> 400; usuário inexistente -> 422.
    ///    Em Embarcador + Agenciador, o mesmo ID_Usuario vale para os dois perfis.
    ///  - Sem coluna de data de alteração em tblpessoa, a edição simultânea é "última gravação vence".
    /// </summary>
    public class PessoaService : IPessoaService
    {
        private const int TamanhoMaximoPagina = 100;

        // Tabela de cada perfil e se ela exige ID_Usuario (NOT NULL no banco).
        private static readonly (PerfilEnum Perfil, string Tabela, bool ExigeUsuario)[] Perfis =
        {
            (PerfilEnum.Embarcador,    "tblcdpessoaembarcador",    true),
            (PerfilEnum.Agenciador,    "tblcdpessoaagenciador",    true),
            (PerfilEnum.Motorista,     "tblcdpessoamotorista",     false),
            (PerfilEnum.Corretor,      "tblcdpessoacorretor",      true),
            (PerfilEnum.Cliente,       "tblcdpessoacliente",       false),
            (PerfilEnum.Fornecedor,    "tblcdpessoafornecedor",    false),
            (PerfilEnum.ProdutorRural, "tblcdpessoaprodutorrural", false),
            (PerfilEnum.ParceiroFrete, "tblcdpessoaparceirofrete", true),
        };

        private static readonly string ListaPerfis = string.Join(", ", Enum.GetNames<PerfilEnum>());

        private readonly ProjetoDbContext _db;
        private readonly ICepService _cepService;
        private readonly IConfiguration _config;
        private readonly ILogger<PessoaService> _logger;

        public PessoaService(ProjetoDbContext db, ICepService cepService, IConfiguration config, ILogger<PessoaService> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _cepService = cepService ?? throw new ArgumentNullException(nameof(cepService));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ================================================================
        // CONSULTAS
        // ================================================================

        public async Task<(IReadOnlyList<TblPessoaResponseDto> items, int total)> GetAllAsync(
            int page, int pageSize, string? search, string? perfil, CancellationToken ct = default)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, TamanhoMaximoPagina);

            var consulta = _db.TblPessoa.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(perfil))
            {
                var perfilEnum = PerfilEnumExtensions.FromString(perfil)
                    ?? throw new ArgumentException($"Perfil '{perfil}' inválido. Use: {ListaPerfis}.");
                var idsDoPerfil = await IdsDoPerfilAsync(perfilEnum, ct).ConfigureAwait(false);
                consulta = consulta.Where(p => idsDoPerfil.Contains(p.ID_Pessoa));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var idsDaBusca = await IdsPorTextoAsync(search.Trim(), ct).ConfigureAwait(false);
                consulta = consulta.Where(p => idsDaBusca.Contains(p.ID_Pessoa));
            }

            var total = await consulta.CountAsync(ct).ConfigureAwait(false);

            // Ordem estável (mais recentes primeiro) para a paginação não repetir nem pular registros.
            var ids = await consulta
                .OrderByDescending(p => p.ID_Pessoa)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => p.ID_Pessoa)
                .ToListAsync(ct).ConfigureAwait(false);

            var itens = await MontarRespostasAsync(ids, ct).ConfigureAwait(false);
            return (itens, total);
        }

        public async Task<TblPessoaResponseDto?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var lista = await MontarRespostasAsync(new List<int> { id }, ct).ConfigureAwait(false);
            return lista.FirstOrDefault();
        }

        public async Task<IReadOnlyList<string>> GetPerfisAsync(int id, CancellationToken ct = default)
        {
            await GarantirPessoaExisteAsync(id, ct).ConfigureAwait(false);
            var mapa = await PerfisPorPessoaAsync(new List<int> { id }, ct).ConfigureAwait(false);
            return mapa.TryGetValue(id, out var lista) ? lista : new List<string>();
        }

        // ================================================================
        // CADASTRO
        // ================================================================

        public async Task<int> CreatePessoaAsync(TblPessoaRequestDto dto, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(dto);

            var isFisica = ValidarTipoEDocumento(dto);
            var perfis = ValidarPerfis(dto.Perfis, dto.NomeFantasia);
            var idUsuario = perfis.Any(ExigeUsuario) ? await ValidarUsuarioAsync(dto.ID_Usuario, ct).ConfigureAwait(false) : 0;

            // CEP e ViaCEP ficam FORA da transação (rede lenta não pode segurar conexão aberta).
            var cep = await _cepService.ResolverAsync(dto.Cep, dto.Logradouro, dto.Bairro, dto.Cidade, dto.Uf, ct)
                                       .ConfigureAwait(false);

            await using var tx = await _db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
            try
            {
                var pessoa = new TblPessoa
                {
                    ID_Cep = cep.ID_Cep,
                    CodigoPessoaTipo = isFisica ? 1 : 2,
                    Numero = dto.Numero.Trim(),
                    ComplementoNumero = dto.ComplementoNumero?.Trim() ?? string.Empty,
                    DddFixo = string.Empty,
                    Fixo = string.Empty,
                    DddCelular = dto.DddCelular,
                    Celular = dto.Celular,
                    Contato = dto.Contato.Trim(),
                    Estatus = EstatusEnum.Ativo
                };

                _db.TblPessoa.Add(pessoa);
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
                var idPessoa = pessoa.ID_Pessoa;

                if (isFisica)
                {
                    _db.TblPessoaFisica.Add(new TblPessoaFisica
                    {
                        ID_Pessoa = idPessoa,
                        Nome = dto.Nome.Trim(),
                        Cpf = dto.Cpf!,
                        Rg = dto.Rg ?? string.Empty
                    });
                }
                else
                {
                    _db.TblPessoaJuridica.Add(new TblPessoaJuridica
                    {
                        ID_Pessoa = idPessoa,
                        RazaoSocial = dto.Nome.Trim(),
                        Cnpj = dto.Cnpj!,
                        InscricaoEstadual = dto.InscricaoEstadual ?? string.Empty
                    });
                }

                var email = new TblPessoaEmail
                {
                    ID_Pessoa = idPessoa,
                    Setor = SetorEnum.Cadastro,
                    Email = dto.Email.Trim(),
                    SelecionadoParaImprimir = SimNaoEnum.Nao
                };
                _db.TblPessoaEmail.Add(email);

                // Grava física/jurídica e e-mail (a unicidade de CPF/CNPJ é garantida pelo banco).
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);

                // A pessoa aponta para o e-mail de cadastro (a view uvwpessoa depende disto).
                await _db.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE tblpessoa SET ID_PessoaEmailCadastro = {email.ID_PessoaEmail} WHERE ID_Pessoa = {idPessoa}", ct)
                    .ConfigureAwait(false);

                foreach (var p in perfis)
                    await InserirPerfilAsync(idPessoa, p, dto.NomeFantasia, dto.Pis, idUsuario, ct).ConfigureAwait(false);

                await tx.CommitAsync(ct).ConfigureAwait(false);

                _logger.LogInformation("Pessoa {Id} criada ({Tipo}) com {Qtd} perfil(is).", idPessoa, isFisica ? "F" : "J", perfis.Count);
                return idPessoa;
            }
            catch (Exception ex) when (EhDuplicidade(ex))
            {
                throw new ConflitoServiceException(isFisica
                    ? "Já existe uma pessoa cadastrada com este CPF."
                    : "Já existe uma pessoa cadastrada com este CNPJ.");
            }
        }

        public async Task UpdatePessoaAsync(int id, TblPessoaRequestDto dto, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(dto);

            var isFisica = ValidarTipoEDocumento(dto);

            var atual = await _db.TblPessoa.AsNoTracking()
                .Where(p => p.ID_Pessoa == id)
                .Select(p => new { p.CodigoPessoaTipo })
                .FirstOrDefaultAsync(ct).ConfigureAwait(false)
                ?? throw new KeyNotFoundException($"Pessoa {id} não encontrada.");

            if ((atual.CodigoPessoaTipo == 1) != isFisica)
                throw new RegraNegocioServiceException("O tipo da pessoa (física/jurídica) não pode ser alterado.");

            // CPF/CNPJ não mudam por aqui (compara só os dígitos: o banco pode ter valores sem máscara).
            if (isFisica)
            {
                var cpfAtual = await _db.TblPessoaFisica.AsNoTracking()
                    .Where(f => f.ID_Pessoa == id).Select(f => f.Cpf).FirstOrDefaultAsync(ct).ConfigureAwait(false);
                if (cpfAtual != null && SoDigitos(cpfAtual) != SoDigitos(dto.Cpf!))
                    throw new RegraNegocioServiceException("O CPF não pode ser alterado por esta operação.");
            }
            else
            {
                var cnpjAtual = await _db.TblPessoaJuridica.AsNoTracking()
                    .Where(j => j.ID_Pessoa == id).Select(j => j.Cnpj).FirstOrDefaultAsync(ct).ConfigureAwait(false);
                if (cnpjAtual != null && SoDigitos(cnpjAtual) != SoDigitos(dto.Cnpj!))
                    throw new RegraNegocioServiceException("O CNPJ não pode ser alterado por esta operação.");
            }

            var cep = await _cepService.ResolverAsync(dto.Cep, dto.Logradouro, dto.Bairro, dto.Cidade, dto.Uf, ct)
                                       .ConfigureAwait(false);

            var nome = dto.Nome.Trim();
            var numero = dto.Numero.Trim();
            var complemento = dto.ComplementoNumero?.Trim() ?? string.Empty;
            var contato = dto.Contato.Trim();
            var emailNovo = dto.Email.Trim();

            await using var tx = await _db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

            await _db.TblPessoa.Where(p => p.ID_Pessoa == id).ExecuteUpdateAsync(s => s
                .SetProperty(p => p.ID_Cep, cep.ID_Cep)
                .SetProperty(p => p.Numero, numero)
                .SetProperty(p => p.ComplementoNumero, complemento)
                .SetProperty(p => p.DddCelular, dto.DddCelular)
                .SetProperty(p => p.Celular, dto.Celular)
                .SetProperty(p => p.Contato, contato), ct).ConfigureAwait(false);

            if (isFisica)
            {
                var rg = dto.Rg ?? string.Empty;
                await _db.TblPessoaFisica.Where(f => f.ID_Pessoa == id).ExecuteUpdateAsync(s => s
                    .SetProperty(f => f.Nome, nome)
                    .SetProperty(f => f.Rg, rg), ct).ConfigureAwait(false);
            }
            else
            {
                var ie = dto.InscricaoEstadual ?? string.Empty;
                await _db.TblPessoaJuridica.Where(j => j.ID_Pessoa == id).ExecuteUpdateAsync(s => s
                    .SetProperty(j => j.RazaoSocial, nome)
                    .SetProperty(j => j.InscricaoEstadual, ie), ct).ConfigureAwait(false);
            }

            await AtualizarEmailCadastroAsync(id, emailNovo, ct).ConfigureAwait(false);

            await tx.CommitAsync(ct).ConfigureAwait(false);
            _logger.LogInformation("Pessoa {Id} atualizada.", id);
        }

        // ================================================================
        // SITUAÇÃO (nunca apaga)
        // ================================================================

        public async Task InativarAsync(int id, CancellationToken ct = default)
        {
            await GarantirPessoaExisteAsync(id, ct).ConfigureAwait(false);
            await _db.TblPessoa.Where(p => p.ID_Pessoa == id)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Estatus, EstatusEnum.Inativo), ct).ConfigureAwait(false);
            _logger.LogInformation("Pessoa {Id} inativada.", id);
        }

        public async Task ReativarAsync(int id, CancellationToken ct = default)
        {
            await GarantirPessoaExisteAsync(id, ct).ConfigureAwait(false);
            await _db.TblPessoa.Where(p => p.ID_Pessoa == id)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Estatus, EstatusEnum.Ativo), ct).ConfigureAwait(false);
            _logger.LogInformation("Pessoa {Id} reativada.", id);
        }

        // ================================================================
        // PERFIS
        // ================================================================

        public async Task AdicionarPerfilAsync(int id, string perfil, PessoaPerfilRequestDto? dados, CancellationToken ct = default)
        {
            var perfilEnum = PerfilEnumExtensions.FromString(perfil)
                ?? throw new ArgumentException($"Perfil '{perfil}' inválido. Use: {ListaPerfis}.");

            if (perfilEnum == PerfilEnum.Fornecedor && string.IsNullOrWhiteSpace(dados?.NomeFantasia))
                throw new ArgumentException("Perfil Fornecedor exige NomeFantasia.");

            var idUsuario = ExigeUsuario(perfilEnum) ? await ValidarUsuarioAsync(dados?.ID_Usuario, ct).ConfigureAwait(false) : 0;

            await using var tx = await _db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

            // Trava a linha da pessoa até o fim da transação: duas requisições simultâneas não
            // conseguem criar perfis incompatíveis. (Também confirma que a pessoa existe.)
            var travada = await _db.Database
                .SqlQuery<long>($"SELECT CAST(ID_Pessoa AS SIGNED) AS Value FROM tblpessoa WHERE ID_Pessoa = {id} FOR UPDATE")
                .ToListAsync(ct).ConfigureAwait(false);
            if (travada.Count == 0)
                throw new KeyNotFoundException($"Pessoa {id} não encontrada.");

            var mapa = await PerfisPorPessoaAsync(new List<int> { id }, ct).ConfigureAwait(false);
            var existentes = (mapa.TryGetValue(id, out var lista) ? lista : new List<string>())
                .Select(n => Enum.Parse<PerfilEnum>(n)).ToHashSet();

            if (existentes.Contains(perfilEnum))
                throw new ConflitoServiceException($"A pessoa já possui o perfil {perfilEnum}.");

            var resultado = new HashSet<PerfilEnum>(existentes) { perfilEnum };
            ValidarCombinacao(resultado);

            await InserirPerfilAsync(id, perfilEnum, dados?.NomeFantasia, dados?.Pis, idUsuario, ct).ConfigureAwait(false);

            await tx.CommitAsync(ct).ConfigureAwait(false);
            _logger.LogInformation("Perfil {Perfil} adicionado à pessoa {Id}.", perfilEnum, id);
        }

        // Remoção de perfil: NÃO implementada de propósito. Só pode ocorrer se o perfil ainda não foi
        // usado (lotes, embarques etc.); a lista de tabelas que contam como "uso" depende da ação
        // Dependencias do utilitário de banco e de decisão do negócio.

        // ================================================================
        // AUXILIARES — perfis
        // ================================================================

        private static bool ExigeUsuario(PerfilEnum perfil) => Perfis.First(p => p.Perfil == perfil).ExigeUsuario;

        private static void ValidarCombinacao(IReadOnlyCollection<PerfilEnum> perfis)
        {
            if (perfis.Count <= 1) return;

            var permitido = perfis.Count == 2
                            && perfis.Contains(PerfilEnum.Embarcador)
                            && perfis.Contains(PerfilEnum.Agenciador);

            if (!permitido)
                throw new RegraNegocioServiceException(
                    "Combinação de perfis inválida: uma pessoa tem no máximo um perfil, exceto Embarcador + Agenciador.");
        }

        private static HashSet<PerfilEnum> ValidarPerfis(IEnumerable<string>? nomes, string? nomeFantasia)
        {
            var set = new HashSet<PerfilEnum>();
            foreach (var nome in nomes ?? Enumerable.Empty<string>())
            {
                var p = PerfilEnumExtensions.FromString(nome)
                    ?? throw new ArgumentException($"Perfil '{nome}' inválido. Use: {ListaPerfis}.");
                set.Add(p);
            }

            ValidarCombinacao(set);

            if (set.Contains(PerfilEnum.Fornecedor) && string.IsNullOrWhiteSpace(nomeFantasia))
                throw new ArgumentException("Perfil Fornecedor exige NomeFantasia.");

            return set;
        }

        /// <summary>
        /// Perfis Embarcador, Agenciador, Corretor e ParceiroFrete ficam ligados a um usuário da tblusuario.
        /// Confere que o ID_Usuario foi informado e existe.
        /// </summary>
        private async Task<int> ValidarUsuarioAsync(int? idUsuario, CancellationToken ct)
        {
            if (idUsuario is null or <= 0)
                throw new ArgumentException(
                    "Embarcador, Agenciador, Corretor e ParceiroFrete exigem ID_Usuario (usuário ao qual a pessoa será ligada).");

            var id = idUsuario.Value;
            var achou = await _db.Database
                .SqlQuery<long>($"SELECT CAST(ID_Usuario AS SIGNED) AS Value FROM tblusuario WHERE ID_Usuario = {id}")
                .ToListAsync(ct).ConfigureAwait(false);
            if (achou.Count == 0)
                throw new RegraNegocioServiceException($"Usuário {id} não encontrado em tblusuario.");
            return id;
        }

        /// <summary>INSERT do perfil por SQL parametrizado (colunas verificadas no banco real).</summary>
        private async Task InserirPerfilAsync(int idPessoa, PerfilEnum perfil, string? nomeFantasia, string? pis,
                                              int idUsuario, CancellationToken ct)
        {
            switch (perfil)
            {
                case PerfilEnum.Cliente:
                    await _db.Database.ExecuteSqlInterpolatedAsync(
                        $"INSERT INTO tblcdpessoacliente (ID_Pessoa) VALUES ({idPessoa})", ct).ConfigureAwait(false);
                    break;

                case PerfilEnum.Motorista:
                    await _db.Database.ExecuteSqlInterpolatedAsync(
                        $"INSERT INTO tblcdpessoamotorista (ID_Pessoa) VALUES ({idPessoa})", ct).ConfigureAwait(false);
                    break;

                case PerfilEnum.Fornecedor:
                    var fantasia = nomeFantasia!.Trim();
                    await _db.Database.ExecuteSqlInterpolatedAsync(
                        $"INSERT INTO tblcdpessoafornecedor (ID_Pessoa, NomeFantasia) VALUES ({idPessoa}, {fantasia})", ct).ConfigureAwait(false);
                    break;

                case PerfilEnum.ProdutorRural:
                    var pisLimpo = string.IsNullOrWhiteSpace(pis) ? null : pis.Trim();
                    await _db.Database.ExecuteSqlInterpolatedAsync(
                        $"INSERT INTO tblcdpessoaprodutorrural (ID_Pessoa, Pis) VALUES ({idPessoa}, {pisLimpo})", ct).ConfigureAwait(false);
                    break;

                case PerfilEnum.Embarcador:
                    await _db.Database.ExecuteSqlInterpolatedAsync(
                        $"INSERT INTO tblcdpessoaembarcador (ID_Pessoa, ID_Usuario) VALUES ({idPessoa}, {idUsuario})", ct).ConfigureAwait(false);
                    break;

                case PerfilEnum.Agenciador:
                    await _db.Database.ExecuteSqlInterpolatedAsync(
                        $"INSERT INTO tblcdpessoaagenciador (ID_Pessoa, ID_Usuario) VALUES ({idPessoa}, {idUsuario})", ct).ConfigureAwait(false);
                    break;

                case PerfilEnum.Corretor:
                    await _db.Database.ExecuteSqlInterpolatedAsync(
                        $"INSERT INTO tblcdpessoacorretor (ID_Pessoa, ID_Usuario) VALUES ({idPessoa}, {idUsuario})", ct).ConfigureAwait(false);
                    break;

                case PerfilEnum.ParceiroFrete:
                    await _db.Database.ExecuteSqlInterpolatedAsync(
                        $"INSERT INTO tblcdpessoaparceirofrete (ID_Pessoa, ID_Usuario) VALUES ({idPessoa}, {idUsuario})", ct).ConfigureAwait(false);
                    break;

                default:
                    throw new ArgumentException($"Perfil '{perfil}' não implementado.");
            }
        }

        /// <summary>IDs (tblpessoa.ID_Pessoa) que possuem determinado perfil.</summary>
        private async Task<List<int>> IdsDoPerfilAsync(PerfilEnum perfil, CancellationToken ct)
        {
            var tabela = Perfis.First(p => p.Perfil == perfil).Tabela;   // constante, nunca vem do usuário
            var ids = await _db.Database
                .SqlQueryRaw<long>("SELECT CAST(ID_Pessoa AS SIGNED) AS Value FROM " + tabela)
                .ToListAsync(ct).ConfigureAwait(false);
            return ids.Select(i => (int)i).ToList();
        }

        /// <summary>Perfis de várias pessoas em UMA consulta (uma linha por pessoa × perfil).</summary>
        private async Task<Dictionary<int, List<string>>> PerfisPorPessoaAsync(IReadOnlyCollection<int> ids, CancellationToken ct)
        {
            var resultado = new Dictionary<int, List<string>>();
            if (ids.Count == 0) return resultado;

            var csv = string.Join(",", ids);   // inteiros que vieram do próprio banco
            var sql = string.Join(" UNION ALL ", Perfis.Select(p =>
                "SELECT '" + p.Perfil + "' AS Perfil, CAST(ID_Pessoa AS SIGNED) AS IdPessoa FROM " + p.Tabela +
                " WHERE ID_Pessoa IN (" + csv + ")"));

            var linhas = await _db.Database.SqlQueryRaw<PerfilLinhaResponseDto>(sql).ToListAsync(ct).ConfigureAwait(false);

            foreach (var grupo in linhas.GroupBy(l => (int)l.IdPessoa))
                resultado[grupo.Key] = grupo.Select(l => l.Perfil)
                                            .OrderBy(n => (int)Enum.Parse<PerfilEnum>(n))
                                            .ToList();
            return resultado;
        }

        // ================================================================
        // AUXILIARES — busca e montagem da resposta
        // ================================================================

        /// <summary>Pessoas cujo nome/razão social ou CPF/CNPJ contém o texto.</summary>
        private async Task<List<int>> IdsPorTextoAsync(string texto, CancellationToken ct)
        {
            // MySQL usa "\" como escape padrão no LIKE: neutraliza \, % e _ digitados pelo usuário.
            var padrao = "%" + texto.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";

            var fisicas = await _db.TblPessoaFisica.AsNoTracking()
                .Where(f => EF.Functions.Like(f.Nome, padrao) || EF.Functions.Like(f.Cpf, padrao))
                .Select(f => f.ID_Pessoa).ToListAsync(ct).ConfigureAwait(false);

            var juridicas = await _db.TblPessoaJuridica.AsNoTracking()
                .Where(j => EF.Functions.Like(j.RazaoSocial, padrao) || EF.Functions.Like(j.Cnpj, padrao))
                .Select(j => j.ID_Pessoa).ToListAsync(ct).ConfigureAwait(false);

            return fisicas.Concat(juridicas).Select(i => (int)i).Distinct().ToList();
        }

        private async Task GarantirPessoaExisteAsync(int id, CancellationToken ct)
        {
            if (!await _db.TblPessoa.AsNoTracking().AnyAsync(p => p.ID_Pessoa == id, ct).ConfigureAwait(false))
                throw new KeyNotFoundException($"Pessoa {id} não encontrada.");
        }

        /// <summary>
        /// Monta as respostas na ordem dos IDs recebidos, com poucas consultas em lote.
        /// Só lê colunas necessárias (projeções): não depende de colunas opcionais das entidades.
        /// </summary>
        private async Task<List<TblPessoaResponseDto>> MontarRespostasAsync(List<int> ids, CancellationToken ct)
        {
            var resultado = new List<TblPessoaResponseDto>(ids.Count);
            if (ids.Count == 0) return resultado;

            var pessoas = (await _db.TblPessoa.AsNoTracking()
                .Where(p => ids.Contains(p.ID_Pessoa))
                .Select(p => new { p.ID_Pessoa, p.CodigoPessoaTipo, p.ID_Cep, p.Numero, p.ComplementoNumero, p.DddCelular, p.Celular, p.Contato, p.Estatus })
                .ToListAsync(ct).ConfigureAwait(false))
                .ToDictionary(p => p.ID_Pessoa);

            var fisicas = (await _db.TblPessoaFisica.AsNoTracking()
                .Where(f => ids.Contains(f.ID_Pessoa))
                .Select(f => new { f.ID_Pessoa, f.Nome, f.Cpf, f.Rg })
                .ToListAsync(ct).ConfigureAwait(false))
                .GroupBy(f => f.ID_Pessoa).ToDictionary(g => g.Key, g => g.First());

            var juridicas = (await _db.TblPessoaJuridica.AsNoTracking()
                .Where(j => ids.Contains(j.ID_Pessoa))
                .Select(j => new { j.ID_Pessoa, j.RazaoSocial, j.Cnpj, j.InscricaoEstadual })
                .ToListAsync(ct).ConfigureAwait(false))
                .GroupBy(j => j.ID_Pessoa).ToDictionary(g => g.Key, g => g.First());

            // Só o e-mail do setor Cadastro: não lê e-mails de outros setores.
            var emails = (await _db.TblPessoaEmail.AsNoTracking()
                .Where(e => ids.Contains(e.ID_Pessoa) && e.Setor == SetorEnum.Cadastro)
                .Select(e => new { e.ID_Pessoa, e.ID_PessoaEmail, e.Email })
                .ToListAsync(ct).ConfigureAwait(false))
                .GroupBy(e => e.ID_Pessoa).ToDictionary(g => g.Key, g => g.OrderBy(e => e.ID_PessoaEmail).First());

            var cepIds = pessoas.Values.Select(p => p.ID_Cep).Distinct().ToList();
            var ceps = (await _db.TblCep.AsNoTracking()
                .Where(c => cepIds.Contains(c.ID_Cep))
                .Select(c => new { c.ID_Cep, c.Cep, c.ID_Logradouro, c.ID_Bairro, c.ID_Cidade, c.ID_Estado })
                .ToListAsync(ct).ConfigureAwait(false))
                .GroupBy(c => c.ID_Cep).ToDictionary(g => g.Key, g => g.First());

            var logIds = ceps.Values.Select(c => c.ID_Logradouro).Distinct().ToList();
            var logradouros = (await _db.TblLogradouro.AsNoTracking()
                .Where(l => logIds.Contains(l.ID_Logradouro))
                .Select(l => new { l.ID_Logradouro, Descricao = EF.Property<string>(l, "Descricao") })
                .ToListAsync(ct).ConfigureAwait(false))
                .ToDictionary(l => l.ID_Logradouro, l => l.Descricao);

            var bairroIds = ceps.Values.Select(c => c.ID_Bairro).Distinct().ToList();
            var bairros = (await _db.TblBairro.AsNoTracking()
                .Where(b => bairroIds.Contains(b.ID_Bairro))
                .Select(b => new { b.ID_Bairro, Descricao = EF.Property<string>(b, "Descricao") })
                .ToListAsync(ct).ConfigureAwait(false))
                .ToDictionary(b => b.ID_Bairro, b => b.Descricao);

            var cidadeIds = ceps.Values.Select(c => c.ID_Cidade).Distinct().ToList();
            var cidades = (await _db.TblCidade.AsNoTracking()
                .Where(c => cidadeIds.Contains(c.ID_Cidade))
                .Select(c => new { c.ID_Cidade, Descricao = EF.Property<string>(c, "Descricao") })
                .ToListAsync(ct).ConfigureAwait(false))
                .ToDictionary(c => c.ID_Cidade, c => c.Descricao);

            var estadoIds = ceps.Values.Select(c => c.ID_Estado).Distinct().ToList();
            var estados = (await _db.TblEstado.AsNoTracking()
                .Where(e => estadoIds.Contains(e.ID_Estado))
                .Select(e => new { e.ID_Estado, e.Uf })
                .ToListAsync(ct).ConfigureAwait(false))
                .ToDictionary(e => e.ID_Estado, e => e.Uf);

            var perfis = await PerfisPorPessoaAsync(ids, ct).ConfigureAwait(false);

            foreach (var id in ids)
            {
                if (!pessoas.TryGetValue(id, out var p)) continue;

                var dto = new TblPessoaResponseDto
                {
                    ID_Pessoa = id,
                    TipoPessoa = p.CodigoPessoaTipo == 1 ? "Fisica" : "Juridica",
                    Numero = p.Numero ?? string.Empty,
                    ComplementoNumero = p.ComplementoNumero ?? string.Empty,
                    DddCelular = p.DddCelular ?? string.Empty,
                    Celular = p.Celular ?? string.Empty,
                    Contato = p.Contato ?? string.Empty,
                    Estatus = p.Estatus,
                    Perfis = perfis.TryGetValue(id, out var lp) ? lp : new List<string>()
                };

                if (fisicas.TryGetValue(id, out var f))
                {
                    dto.Nome = f.Nome;
                    dto.Cpf = f.Cpf;
                    dto.Rg = f.Rg;
                }
                else if (juridicas.TryGetValue(id, out var j))
                {
                    dto.Nome = j.RazaoSocial;
                    dto.Cnpj = j.Cnpj;
                    dto.InscricaoEstadual = j.InscricaoEstadual;
                }

                if (emails.TryGetValue(id, out var em))
                    dto.Email = em.Email;

                if (ceps.TryGetValue(p.ID_Cep, out var c))
                {
                    dto.Cep = c.Cep;
                    dto.Logradouro = logradouros.TryGetValue(c.ID_Logradouro, out var lg) ? lg : string.Empty;
                    dto.Bairro = bairros.TryGetValue(c.ID_Bairro, out var br) ? br : string.Empty;
                    dto.Cidade = cidades.TryGetValue(c.ID_Cidade, out var cd) ? cd : string.Empty;
                    dto.Uf = estados.TryGetValue(c.ID_Estado, out var uf) ? uf : string.Empty;
                }

                resultado.Add(dto);
            }

            return resultado;
        }

        /// <summary>
        /// Atualiza o e-mail de cadastro: usa o e-mail apontado por tblpessoa.ID_PessoaEmailCadastro;
        /// se não houver, usa o primeiro e-mail do setor Cadastro; se também não houver, cria.
        /// </summary>
        private async Task AtualizarEmailCadastroAsync(int idPessoa, string email, CancellationToken ct)
        {
            long idEmail = 0;

            var ponteiro = (await _db.Database.SqlQuery<long>(
                $"SELECT CAST(IFNULL(ID_PessoaEmailCadastro, 0) AS SIGNED) AS Value FROM tblpessoa WHERE ID_Pessoa = {idPessoa}")
                .ToListAsync(ct).ConfigureAwait(false)).FirstOrDefault();

            if (ponteiro > 0)
            {
                idEmail = (await _db.Database.SqlQuery<long>(
                    $"SELECT CAST(ID_PessoaEmail AS SIGNED) AS Value FROM tblpessoaemail WHERE ID_PessoaEmail = {ponteiro} AND ID_Pessoa = {idPessoa}")
                    .ToListAsync(ct).ConfigureAwait(false)).FirstOrDefault();
            }

            if (idEmail == 0)
            {
                idEmail = (await _db.Database.SqlQuery<long>(
                    $"SELECT CAST(ID_PessoaEmail AS SIGNED) AS Value FROM tblpessoaemail WHERE ID_Pessoa = {idPessoa} AND Setor = 'Cadastro' ORDER BY ID_PessoaEmail LIMIT 1")
                    .ToListAsync(ct).ConfigureAwait(false)).FirstOrDefault();
            }

            if (idEmail > 0)
            {
                await _db.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE tblpessoaemail SET email = {email} WHERE ID_PessoaEmail = {idEmail}", ct).ConfigureAwait(false);
            }
            else
            {
                await _db.Database.ExecuteSqlInterpolatedAsync(
                    $"INSERT INTO tblpessoaemail (ID_Pessoa, Setor, email, SelecionadoParaImprimir) VALUES ({idPessoa}, 'Cadastro', {email}, 'Nao')", ct)
                    .ConfigureAwait(false);
                idEmail = (await _db.Database.SqlQuery<long>($"SELECT CAST(LAST_INSERT_ID() AS SIGNED) AS Value")
                    .ToListAsync(ct).ConfigureAwait(false)).First();
            }

            if (ponteiro <= 0 || idEmail != ponteiro)
            {
                // Só reaponta quando o ponteiro estava vazio ou inválido.
                var ponteiroValido = ponteiro > 0 && (await _db.Database.SqlQuery<long>(
                    $"SELECT CAST(ID_PessoaEmail AS SIGNED) AS Value FROM tblpessoaemail WHERE ID_PessoaEmail = {ponteiro}")
                    .ToListAsync(ct).ConfigureAwait(false)).Count > 0;

                if (!ponteiroValido)
                    await _db.Database.ExecuteSqlInterpolatedAsync(
                        $"UPDATE tblpessoa SET ID_PessoaEmailCadastro = {idEmail} WHERE ID_Pessoa = {idPessoa}", ct).ConfigureAwait(false);
            }
        }

        // ================================================================
        // AUXILIARES — validação
        // ================================================================

        /// <summary>Valida tipo e documento; normaliza CPF/CNPJ com máscara. Devolve true se for física.</summary>
        private static bool ValidarTipoEDocumento(TblPessoaRequestDto dto)
        {
            var tipo = dto.TipoPessoa?.Trim();
            if (string.IsNullOrWhiteSpace(tipo))
                throw new ArgumentException("TipoPessoa é obrigatório ('Fisica' ou 'Juridica').");

            bool isFisica = tipo.Equals("Fisica", StringComparison.OrdinalIgnoreCase);
            bool isJuridica = tipo.Equals("Juridica", StringComparison.OrdinalIgnoreCase);
            if (!isFisica && !isJuridica)
                throw new ArgumentException("TipoPessoa inválido. Use 'Fisica' ou 'Juridica'.");

            if (isFisica)
            {
                if (string.IsNullOrWhiteSpace(dto.Cpf))
                    throw new ArgumentException("CPF é obrigatório para pessoa física.");
                if (!CpfCnpjValidator.IsValidCpf(dto.Cpf))
                    throw new ArgumentException("CPF inválido.");
                dto.Cpf = FormatUtils.MaskCpf(dto.Cpf);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(dto.Cnpj))
                    throw new ArgumentException("CNPJ é obrigatório para pessoa jurídica.");
                if (!CpfCnpjValidator.IsValidCnpj(dto.Cnpj))
                    throw new ArgumentException("CNPJ inválido.");
                dto.Cnpj = FormatUtils.MaskCnpj(dto.Cnpj);
            }

            return isFisica;
        }

        private static string SoDigitos(string valor) => new(valor.Where(char.IsDigit).ToArray());

        private static bool EhDuplicidade(Exception ex) =>
            ex.GetBaseException().Message.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Violação de unicidade ou estado conflitante lançada pelo PessoaService (vira HTTP 409).</summary>
    public sealed class ConflitoServiceException : Exception
    {
        public ConflitoServiceException(string mensagem) : base(mensagem) { }
    }

    /// <summary>Regra de negócio violada, lançada pelo PessoaService (vira HTTP 422).</summary>
    public sealed class RegraNegocioServiceException : Exception
    {
        public RegraNegocioServiceException(string mensagem) : base(mensagem) { }
    }
}
