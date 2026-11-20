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
    /// Endereço: tblcep → tbllogradouro, tblbairro, tblcidade, tblestado (estrutura verificada em 2026-11-19).
    /// - Um CEP é gravado UMA vez e reaproveitado por várias pessoas (tblpessoa.ID_Cep).
    /// - Não existe índice único em tblcep nem nas descrições: a criação é serializada por uma trava em
    ///   memória (vale para este servidor) e a busca por descrição usa igualdade exata (collation do banco
    ///   ignora maiúsculas/acentos).
    /// - Os 27 estados já existem: a API só CONSULTA por UF, nunca cria estado.
    /// - Propriedades sem prova de compilação (Descricao) são acessadas por nome via EF.Property / Entry.
    /// </summary>
    public class CepService : ICepService
    {
        private static readonly SemaphoreSlim TravaCriacao = new(1, 1);

        private readonly ProjetoDbContext _db;
        private readonly ICepLookupService _cepLookup;
        private readonly ILogger<CepService> _logger;

        public CepService(ProjetoDbContext db, ICepLookupService cepLookup, ILogger<CepService> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _cepLookup = cepLookup ?? throw new ArgumentNullException(nameof(cepLookup));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<TblCep> ResolverAsync(string cep, string? logradouro, string? bairro, string? cidade, string? uf,
                                                CancellationToken ct = default)
        {
            var cepMascarado = FormatUtils.MaskCep(cep);

            var existente = await BuscarExistenteAsync(cepMascarado, ct).ConfigureAwait(false);
            if (existente != null) return existente;

            // ViaCEP (até 10 s): sempre FORA de qualquer transação.
            var consulta = await _cepLookup.LookupCepAsync(cepMascarado.Replace("-", "")).ConfigureAwait(false);

            bool wsTemLogradouro = !string.IsNullOrWhiteSpace(consulta?.Logradouro);
            bool wsTemBairro = !string.IsNullOrWhiteSpace(consulta?.Bairro);

            var tipoCep = TipoCepEnum.Provisorio;
            if (consulta != null)
            {
                tipoCep = (wsTemLogradouro && wsTemBairro)
                    ? TipoCepEnum.Definitivo
                    : (!string.IsNullOrWhiteSpace(consulta.Cidade) && !string.IsNullOrWhiteSpace(consulta.Estado))
                        ? TipoCepEnum.Unico
                        : TipoCepEnum.Provisorio;
            }

            var logradouroFinal = Limpar(wsTemLogradouro ? consulta!.Logradouro : logradouro, 100, "N/I");
            var bairroFinal = Limpar(wsTemBairro ? consulta!.Bairro : bairro, 50, "N/I");
            var cidadeFinal = Limpar(!string.IsNullOrWhiteSpace(consulta?.Cidade) ? consulta!.Cidade : cidade, 50, null);
            var ufFinal = (!string.IsNullOrWhiteSpace(consulta?.Uf) ? consulta!.Uf : uf)?.Trim().ToUpperInvariant();

            if (cidadeFinal == null)
                throw new ArgumentException("CEP não localizado: informe a Cidade.");
            if (string.IsNullOrWhiteSpace(ufFinal) || ufFinal.Length != 2)
                throw new ArgumentException("CEP não localizado: informe a UF com 2 letras.");

            await TravaCriacao.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                // Confere de novo: outra requisição pode ter criado o mesmo CEP enquanto esta esperava.
                existente = await BuscarExistenteAsync(cepMascarado, ct).ConfigureAwait(false);
                if (existente != null) return existente;

                var estado = await _db.TblEstado.AsNoTracking()
                    .Where(e => e.Uf == ufFinal)
                    .Select(e => new { e.ID_Estado })
                    .FirstOrDefaultAsync(ct).ConfigureAwait(false);
                if (estado == null)
                    throw new ArgumentException($"UF '{ufFinal}' inválida.");

                var logradouroEnt = await ObterOuCriarLogradouroAsync(logradouroFinal, ct).ConfigureAwait(false);
                var bairroEnt = await ObterOuCriarBairroAsync(bairroFinal, ct).ConfigureAwait(false);
                var cidadeEnt = await ObterOuCriarCidadeAsync(cidadeFinal, ct).ConfigureAwait(false);

                var novo = new TblCep
                {
                    Cep = cepMascarado,
                    ID_Logradouro = logradouroEnt.ID_Logradouro,
                    ID_Bairro = bairroEnt.ID_Bairro,
                    ID_Cidade = cidadeEnt.ID_Cidade,
                    ID_Estado = estado.ID_Estado,
                    TipoCep = tipoCep,
                    Estatus = EstatusEnum.Ativo
                };

                _db.TblCep.Add(novo);
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);

                _logger.LogInformation("CEP {Cep} cadastrado (TipoCep={TipoCep}).", cepMascarado, tipoCep);
                return novo;
            }
            finally
            {
                TravaCriacao.Release();
            }
        }

        public async Task<CepResponseDto?> ConsultarAsync(string cep, CancellationToken ct = default)
        {
            var cepMascarado = FormatUtils.MaskCep(cep);

            // Projeção explícita: não lê a coluna Resultado (hoje nula em todos os CEPs).
            var x = await _db.TblCep.AsNoTracking()
                .Where(c => c.Cep == cepMascarado)
                .OrderBy(c => c.ID_Cep)
                .Select(c => new { c.ID_Cep, c.Cep, c.ID_Logradouro, c.ID_Bairro, c.ID_Cidade, c.ID_Estado, c.TipoCep, c.Estatus })
                .FirstOrDefaultAsync(ct).ConfigureAwait(false);

            if (x != null)
            {
                var logradouro = await _db.TblLogradouro.AsNoTracking()
                    .Where(l => l.ID_Logradouro == x.ID_Logradouro)
                    .Select(l => EF.Property<string>(l, "Descricao")).FirstOrDefaultAsync(ct).ConfigureAwait(false);
                var bairro = await _db.TblBairro.AsNoTracking()
                    .Where(b => b.ID_Bairro == x.ID_Bairro)
                    .Select(b => EF.Property<string>(b, "Descricao")).FirstOrDefaultAsync(ct).ConfigureAwait(false);
                var cidade = await _db.TblCidade.AsNoTracking()
                    .Where(ci => ci.ID_Cidade == x.ID_Cidade)
                    .Select(ci => EF.Property<string>(ci, "Descricao")).FirstOrDefaultAsync(ct).ConfigureAwait(false);
                var estado = await _db.TblEstado.AsNoTracking()
                    .Where(e => e.ID_Estado == x.ID_Estado)
                    .Select(e => new { e.Descricao, e.Uf }).FirstOrDefaultAsync(ct).ConfigureAwait(false);

                return new CepResponseDto
                {
                    ID_Cep = (int)x.ID_Cep,
                    Cep = x.Cep,
                    ID_Logradouro = (int)x.ID_Logradouro,
                    ID_Bairro = (int)x.ID_Bairro,
                    ID_Cidade = (int)x.ID_Cidade,
                    ID_Estado = (int)x.ID_Estado,
                    Logradouro = logradouro ?? string.Empty,
                    Bairro = bairro ?? string.Empty,
                    Cidade = cidade ?? string.Empty,
                    Estado = estado?.Descricao ?? string.Empty,
                    Uf = estado?.Uf ?? string.Empty,
                    TipoCep = x.TipoCep,
                    Estatus = x.Estatus
                };
            }

            // Não está no banco: consulta o ViaCEP, mas NÃO grava (ID_Cep = 0).
            var consulta = await _cepLookup.LookupCepAsync(cepMascarado.Replace("-", "")).ConfigureAwait(false);
            if (consulta == null) return null;

            return new CepResponseDto
            {
                ID_Cep = 0,
                Cep = cepMascarado,
                Logradouro = consulta.Logradouro ?? string.Empty,
                Bairro = consulta.Bairro ?? string.Empty,
                Cidade = consulta.Cidade ?? string.Empty,
                Estado = consulta.Estado ?? string.Empty,
                Uf = consulta.Uf ?? string.Empty,
                TipoCep = TipoCepEnum.Provisorio,
                Estatus = EstatusEnum.Ativo
            };
        }

        // ------------------------------------------------------------------
        // Auxiliares
        // ------------------------------------------------------------------

        private async Task<TblCep?> BuscarExistenteAsync(string cepMascarado, CancellationToken ct)
        {
            var x = await _db.TblCep.AsNoTracking()
                .Where(c => c.Cep == cepMascarado)
                .OrderBy(c => c.ID_Cep)
                .Select(c => new { c.ID_Cep, c.TipoCep })
                .FirstOrDefaultAsync(ct).ConfigureAwait(false);

            return x == null ? null : new TblCep { ID_Cep = x.ID_Cep, Cep = cepMascarado, TipoCep = x.TipoCep };
        }

        private async Task<TblLogradouro> ObterOuCriarLogradouroAsync(string descricao, CancellationToken ct)
        {
            var existente = await _db.TblLogradouro
                .FirstOrDefaultAsync(e => EF.Property<string>(e, "Descricao") == descricao, ct).ConfigureAwait(false);
            if (existente != null) return existente;

            var novo = new TblLogradouro();
            _db.TblLogradouro.Add(novo);
            _db.Entry(novo).Property("Descricao").CurrentValue = descricao;
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return novo;
        }

        private async Task<TblBairro> ObterOuCriarBairroAsync(string descricao, CancellationToken ct)
        {
            var existente = await _db.TblBairro
                .FirstOrDefaultAsync(e => EF.Property<string>(e, "Descricao") == descricao, ct).ConfigureAwait(false);
            if (existente != null) return existente;

            var novo = new TblBairro();
            _db.TblBairro.Add(novo);
            _db.Entry(novo).Property("Descricao").CurrentValue = descricao;
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return novo;
        }

        private async Task<TblCidade> ObterOuCriarCidadeAsync(string descricao, CancellationToken ct)
        {
            var existente = await _db.TblCidade
                .FirstOrDefaultAsync(e => EF.Property<string>(e, "Descricao") == descricao, ct).ConfigureAwait(false);
            if (existente != null) return existente;

            var novo = new TblCidade();
            _db.TblCidade.Add(novo);
            _db.Entry(novo).Property("Descricao").CurrentValue = descricao;
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return novo;
        }

        /// <summary>Apara espaços, corta no tamanho da coluna e aplica o valor padrão (ou null se não houver).</summary>
        private static string? Limpar(string? valor, int tamanhoMaximo, string? padrao)
        {
            var v = string.IsNullOrWhiteSpace(valor) ? null : string.Join(' ', valor.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            if (v == null) return padrao;
            return v.Length > tamanhoMaximo ? v[..tamanhoMaximo] : v;
        }
    }
}
