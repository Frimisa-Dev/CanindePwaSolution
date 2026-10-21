#nullable enable
using FalcaoPwaShared.Enums;
using FalcaoPwaShared.Models.Dtos.TableDto;
using FalcaoPwaShared.Models.Tbl;
using FalcaoWebServer.Data;
using FalcaoWebServer.Services.Utils;
using FalcaoWebServer.Utils;
using Microsoft.EntityFrameworkCore;

namespace FalcaoWebServer.Services
{
    public class PessoaService : IPessoaService
    {
        private readonly FalcaoDbContext _db;
        private readonly ICepLookupService _cepLookup;
        private readonly ILogger<PessoaService> _logger;

        public PessoaService(
            FalcaoDbContext db,
            ICepLookupService cepLookup,
            ILogger<PessoaService> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _cepLookup = cepLookup ?? throw new ArgumentNullException(nameof(cepLookup));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<(IEnumerable<object> items, int total)> GetAllAsync(int page, int pageSize, string? search = null)
        {
            var q = _db.UvwPessoa.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                q = q.Where(p =>
                    EF.Functions.Like(EF.Property<string>(p, "CodigoPessoaTipo") ?? "", $"%{search}%") ||
                    EF.Functions.Like(EF.Property<string>(p, "NomeRazaoSocial") ?? "", $"%{search}%") ||
                    EF.Functions.Like(EF.Property<string>(p, "CpfCnpj") ?? "", $"%{search}%") ||
                    EF.Functions.Like(EF.Property<string>(p, "RgInscricaoEstadual") ?? "", $"%{search}%") ||
                    EF.Functions.Like(EF.Property<string>(p, "NascimentoFundacao") ?? "", $"%{search}%") ||
                    EF.Functions.Like(EF.Property<string>(p, "Cep") ?? "", $"%{search}%") ||
                    EF.Functions.Like(EF.Property<string>(p, "Logradouro") ?? "", $"%{search}%") ||
                    EF.Functions.Like(EF.Property<string>(p, "Numero") ?? "", $"%{search}%") ||
                    EF.Functions.Like(EF.Property<string>(p, "ComplementoNumero") ?? "", $"%{search}%") ||
                    EF.Functions.Like(EF.Property<string>(p, "Bairro") ?? "", $"%{search}%") ||
                    EF.Functions.Like(EF.Property<string>(p, "Cidade") ?? "", $"%{search}%") ||
                    EF.Functions.Like(EF.Property<string>(p, "Uf") ?? "", $"%{search}%") ||
                    EF.Functions.Like(EF.Property<string>(p, "TipoCep") ?? "", $"%{search}%") ||
                    EF.Functions.Like(EF.Property<string>(p, "DddFixo") ?? "", $"%{search}%") ||
                    EF.Functions.Like(EF.Property<string>(p, "Fixo") ?? "", $"%{search}%") ||
                    EF.Functions.Like(EF.Property<string>(p, "DddCelular") ?? "", $"%{search}%") ||
                    EF.Functions.Like(EF.Property<string>(p, "Celular") ?? "", $"%{search}%") ||
                    EF.Functions.Like(EF.Property<string>(p, "Contato") ?? "", $"%{search}%") ||
                    EF.Functions.Like(EF.Property<string?>(p, "Estatus").ToString() ?? "", $"%{search}%")
                );
            }

            var total = await q.CountAsync().ConfigureAwait(false);
            var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync().ConfigureAwait(false);
            return (items.Cast<object>(), total);
        }

        public async Task<object?> GetByIdAsync(int id)
        {
            var found = await _db.UvwPessoa.FindAsync(id).ConfigureAwait(false);
            return found;
        }

        public async Task<int> CreatePessoaAsync(TblPessoaDto dto)
        {
            using var tx = await _db.Database.BeginTransactionAsync().ConfigureAwait(false);

            try
            {
                if (dto == null) throw new ArgumentNullException(nameof(dto));
                _logger.LogInformation("Iniciando criação de pessoa: {Nome}", dto.Nome);

                var tipoPessoa = dto.TipoPessoa?.Trim();
                if (string.IsNullOrWhiteSpace(tipoPessoa))
                    throw new ArgumentException("TipoPessoa é obrigatório ('Fisica' ou 'Juridica').");

                bool isFisica = tipoPessoa.Equals("Fisica", StringComparison.OrdinalIgnoreCase);
                bool isJuridica = tipoPessoa.Equals("Juridica", StringComparison.OrdinalIgnoreCase);
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

                if (string.IsNullOrWhiteSpace(dto.Nome)) throw new ArgumentException("Nome é obrigatório.");
                if (string.IsNullOrWhiteSpace(dto.Cep)) throw new ArgumentException("Cep é obrigatório.");
                if (string.IsNullOrWhiteSpace(dto.Numero)) throw new ArgumentException("Numero é obrigatório.");
                if (string.IsNullOrWhiteSpace(dto.DddCelular)) throw new ArgumentException("DddCelular é obrigatório.");
                if (string.IsNullOrWhiteSpace(dto.Celular)) throw new ArgumentException("Celular é obrigatório.");
                if (string.IsNullOrWhiteSpace(dto.Email)) throw new ArgumentException("Email é obrigatório.");
                if (string.IsNullOrWhiteSpace(dto.Contato)) throw new ArgumentException("Contato é obrigatório.");
                if (string.IsNullOrWhiteSpace(dto.Perfil)) throw new ArgumentException("Perfil é obrigatório.");

                dto.Cep = FormatUtils.MaskCep(dto.Cep);

                TblCep? cepEntry = await _db.TblCep.FirstOrDefaultAsync(c => c.Cep == dto.Cep).ConfigureAwait(false);
                bool encontrado = cepEntry != null;
                var tipoCep = TipoCepEnum.Provisorio;

                if (encontrado)
                {
                    tipoCep = cepEntry!.TipoCep;
                    _logger.LogInformation("CEP {Cep} encontrado localmente (TipoCep={TipoCep}).", dto.Cep, tipoCep);
                }

                if (!encontrado)
                {
                    var lookup = await _cepLookup.LookupCepAsync(dto.Cep.Replace("-", "")).ConfigureAwait(false);
                    if (lookup != null)
                    {
                        encontrado = true;

                        bool wsTemLogradouro = !string.IsNullOrWhiteSpace(lookup.Logradouro);
                        bool wsTemBairro = !string.IsNullOrWhiteSpace(lookup.Bairro);

                        tipoCep = (wsTemLogradouro && wsTemBairro)
                            ? TipoCepEnum.Definitivo
                            : (!string.IsNullOrWhiteSpace(lookup.Cidade) && !string.IsNullOrWhiteSpace(lookup.Estado))
                                ? TipoCepEnum.Unico
                                : TipoCepEnum.Provisorio;

                        _logger.LogInformation("CEP {Cep} encontrado via WebService (TipoCep={TipoCep}).", dto.Cep, tipoCep);

                        var logradouroUsar = wsTemLogradouro ? lookup.Logradouro : dto.Logradouro;
                        var bairroUsar = wsTemBairro ? lookup.Bairro : dto.Bairro;
                        var cidadeUsar = !string.IsNullOrWhiteSpace(lookup.Cidade) ? lookup.Cidade : dto.Cidade;
                        var ufUsar = !string.IsNullOrWhiteSpace(lookup.Uf) ? lookup.Uf : dto.Uf;

                        cepEntry = await CriarOuObterCepAsync(dto.Cep, logradouroUsar, bairroUsar, cidadeUsar, ufUsar, tipoCep)
                            .ConfigureAwait(false);
                    }
                }

                if (!encontrado)
                {
                    _logger.LogWarning("CEP {Cep} não encontrado no banco nem no serviço. Usando dados do DTO.", dto.Cep);
                    cepEntry = await CriarOuObterCepAsync(dto.Cep, dto.Logradouro, dto.Bairro, dto.Cidade, dto.Uf, TipoCepEnum.Provisorio)
                        .ConfigureAwait(false);
                }

                if (cepEntry == null)
                    throw new InvalidOperationException("Não foi possível obter/gerar entrada de CEP.");

                var pessoa = new TblPessoa
                {
                    ID_Cep = cepEntry.ID_Cep,
                    CodigoPessoaTipo = isFisica ? 1 : 2,
                    Numero = dto.Numero,
                    ComplementoNumero = dto.ComplementoNumero ?? string.Empty,
                    DddFixo = string.Empty,
                    Fixo = string.Empty,
                    DddCelular = dto.DddCelular,
                    Celular = dto.Celular,
                    Contato = dto.Contato,
                    Estatus = EstatusEnum.Ativo
                };

                _db.TblPessoa.Add(pessoa);
                await _db.SaveChangesAsync().ConfigureAwait(false);

                if (isFisica)
                {
                    _db.TblPessoaFisica.Add(new TblPessoaFisica
                    {
                        ID_Pessoa = pessoa.ID_Pessoa,
                        Nome = dto.Nome,
                        Cpf = dto.Cpf ?? string.Empty,
                        Rg = dto.Rg ?? string.Empty,
                        DataNascimento = DateTime.MinValue
                    });
                }
                else
                {
                    _db.TblPessoaJuridica.Add(new TblPessoaJuridica
                    {
                        ID_Pessoa = pessoa.ID_Pessoa,
                        RazaoSocial = dto.Nome,
                        Cnpj = dto.Cnpj ?? string.Empty,
                        InscricaoEstadual = dto.InscricaoEstadual ?? string.Empty,
                        DataFundacao = DateTime.MinValue
                    });
                }

                var perfilLower = dto.Perfil?.ToLowerInvariant() ?? string.Empty;
                var setorEspecial = "cadastro";

                if (perfilLower.Contains("cliente"))
                {
                    setorEspecial = "cadastro";
                    _db.TblPessoaCliente.Add(new TblPessoaCliente { ID_Pessoa = pessoa.ID_Pessoa });
                }
               
                // converte string setorEspecial para Enum
                var setorEspecialEnum = UtilizarEnum.ParseOrDefault(setorEspecial, SetorEnum.Cadastro);
                
                _db.TblPessoaEmail.AddRange(
                    new TblPessoaEmail
                    {
                        ID_Pessoa = pessoa.ID_Pessoa,
                        Setor = SetorEnum.Cadastro,
                        Email = dto.Email,
                        SelecionadoParaImprimir = SimNaoEnum.Nao
                    },
                    new TblPessoaEmail
                    {
                        ID_Pessoa = pessoa.ID_Pessoa,
                        Setor = setorEspecialEnum,
                        Email = dto.Email,
                        SelecionadoParaImprimir = SimNaoEnum.Nao
                    });

                await _db.SaveChangesAsync().ConfigureAwait(false);
                await tx.CommitAsync().ConfigureAwait(false);

                _logger.LogInformation("Pessoa criada com sucesso. ID: {Id}", pessoa.ID_Pessoa);
                return pessoa.ID_Pessoa;
            }
            catch (Exception)
            {
                await tx.RollbackAsync().ConfigureAwait(false);
                throw;
            }
        }

        public async Task<int> UpdatePessoaAsync(int idPessoa, TblPessoaDto dto)
        {
            using var tx = await _db.Database.BeginTransactionAsync().ConfigureAwait(false);

            try
            {
                var pessoa = await _db.TblPessoa.FindAsync(idPessoa);
                if (pessoa == null)
                    throw new KeyNotFoundException($"Pessoa ID {idPessoa} não encontrada.");

                pessoa.DddCelular = dto.DddCelular;
                pessoa.Celular = dto.Celular;
                pessoa.Contato = dto.Contato;

                var emailCadastro = await _db.TblPessoaEmail.FirstOrDefaultAsync(e => 
                    e.ID_Pessoa == idPessoa && e.Setor == SetorEnum.Cadastro);

                if (emailCadastro != null)
                {
                    emailCadastro.Email = dto.Email;
                    _db.TblPessoaEmail.Update(emailCadastro);
                }
                else
                {
                    _db.TblPessoaEmail.Add(new TblPessoaEmail
                    {
                        ID_Pessoa = idPessoa,
                        Setor = SetorEnum.Cadastro,
                        Email = dto.Email,
                        SelecionadoParaImprimir = SimNaoEnum.Nao
                    });
                }

                await _db.SaveChangesAsync().ConfigureAwait(false);
                await tx.CommitAsync().ConfigureAwait(false);
                return idPessoa;
            }
            catch (Exception)
            {
                await tx.RollbackAsync().ConfigureAwait(false);
                throw;
            }
        }
        private async Task<TblCep> CriarOuObterCepAsync(string cep, string? logradouro, string? bairro, string? cidade, string? uf, TipoCepEnum tipoCep)
        {
            try
            {
                var logradouroEnt = await GetOrCreateDescricaoAsync(_db.TblLogradouro, logradouro, "Logradouro").ConfigureAwait(false);
                var bairroEnt = await GetOrCreateDescricaoAsync(_db.TblBairro, bairro, "Bairro").ConfigureAwait(false);
                var cidadeEnt = await GetOrCreateDescricaoAsync(_db.TblCidade, cidade, "Cidade").ConfigureAwait(false);

                TblEstado estadoEnt;
                if (string.IsNullOrWhiteSpace(uf))
                {
                    estadoEnt = await _db.TblEstado.FirstOrDefaultAsync(e => e.Uf == "N/I").ConfigureAwait(false)
                        ?? new TblEstado { Descricao = "N/I", Uf = "N/I" };
                    if (estadoEnt.ID_Estado == 0)
                        _db.TblEstado.Add(estadoEnt);
                }
                else
                {
                    var ufNormalized = uf.Trim();
                    estadoEnt = await _db.TblEstado.FirstOrDefaultAsync(e => e.Uf.ToLower() == ufNormalized.ToLower()).ConfigureAwait(false)
                        ?? new TblEstado { Descricao = ufNormalized, Uf = ufNormalized };
                    if (estadoEnt.ID_Estado == 0)
                        _db.TblEstado.Add(estadoEnt);
                }

                var novoCep = new TblCep
                {
                    Cep = cep,
                    ID_Logradouro = logradouroEnt.ID_Logradouro,
                    ID_Bairro = bairroEnt.ID_Bairro,
                    ID_Cidade = cidadeEnt.ID_Cidade,
                    ID_Estado = estadoEnt.ID_Estado,
                    TipoCep = tipoCep,
                    Estatus = EstatusEnum.Ativo,
                    Resultado = ResultadoEnum.OK
                };

                _db.TblCep.Add(novoCep);
                await _db.SaveChangesAsync().ConfigureAwait(false);
                return novoCep;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task<T> GetOrCreateDescricaoAsync<T>(DbSet<T> dbSet, string? valorOrigem, string campoOrigem) where T : class, new()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(valorOrigem))
                {
                    _logger.LogWarning("Campo {CampoOrigem} não informado. Substituindo por 'N/I'.", campoOrigem);
                    valorOrigem = "N/I";
                }

                var valorNormalized = valorOrigem.Trim();
                var propDescricao = typeof(T).GetProperty("Descricao")
                    ?? throw new InvalidOperationException($"Tipo {typeof(T).Name} não possui propriedade 'Descricao'.");

                var todos = await dbSet.AsNoTracking().ToListAsync().ConfigureAwait(false);
                var existente = todos.FirstOrDefault(e =>
                {
                    var v = propDescricao.GetValue(e)?.ToString() ?? string.Empty;
                    return string.Equals(v.Trim(), valorNormalized, StringComparison.OrdinalIgnoreCase);
                });

                if (existente != null)
                    return existente;

                var novo = new T();
                propDescricao.SetValue(novo, valorNormalized);
                dbSet.Add(novo);

                await _db.SaveChangesAsync().ConfigureAwait(false);
                return novo;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
