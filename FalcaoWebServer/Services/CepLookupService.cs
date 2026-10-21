#nullable enable
using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FalcaoPwaShared.Models.Tbl;
using FalcaoWebServer.Data;

namespace FalcaoWebServer.Services
{
    public class CepLookupService : ICepLookupService
    {
        private readonly ILogger<CepLookupService> _logger;
        private readonly FalcaoDbContext _db;
        private readonly IHttpClientFactory _httpClientFactory;

        public CepLookupService(ILogger<CepLookupService> logger, FalcaoDbContext db, IHttpClientFactory httpClientFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        /// <summary>
        /// Procura o CEP localmente (TblCep). Se não encontrar, tenta fallback para ViaCEP (ou outro).
        /// Retorna null se não encontrado.
        /// </summary>
        public async Task<CepLookupResult?> LookupCepAsync(string cep)
        {
            if (string.IsNullOrWhiteSpace(cep)) return null;

            // Normaliza: aceita com ou sem hífen e retorna com hífen (ex: 01001-000)
            var digits = new string(cep.Where(char.IsDigit).ToArray());
            if (digits.Length != 8) return null;
            var cepWithHyphen = digits.Insert(5, "-");

            // 1) Procura no banco TblCep (o campo armazenado por sua definição é com hífen)
            var cepEntry = await _db.TblCep.FirstOrDefaultAsync(c => c.Cep == cepWithHyphen).ConfigureAwait(false);
            if (cepEntry != null)
            {
                // Carrega nomes a partir dos IDs relacionados (TblLogradouro, TblBairro, TblCidade, TblEstado)
                var logradouro = string.Empty;
                var bairro = string.Empty;
                var cidade = string.Empty;
                var estado = string.Empty;
                var uf = string.Empty;

                if (cepEntry.ID_Logradouro != 0)
                {
                    var l = await _db.TblLogradouro.FindAsync(cepEntry.ID_Logradouro).ConfigureAwait(false);
                    if (l != null) logradouro = l.Descricao ?? string.Empty;
                }

                if (cepEntry.ID_Bairro != 0)
                {
                    var b = await _db.TblBairro.FindAsync(cepEntry.ID_Bairro).ConfigureAwait(false);
                    if (b != null) bairro = b.Descricao ?? string.Empty;
                }

                if (cepEntry.ID_Cidade != 0)
                {
                    var c = await _db.TblCidade.FindAsync(cepEntry.ID_Cidade).ConfigureAwait(false);
                    if (c != null) cidade = c.Descricao ?? string.Empty;
                }

                if (cepEntry.ID_Estado != 0)
                {
                    var e = await _db.TblEstado.FindAsync(cepEntry.ID_Estado).ConfigureAwait(false);
                    if (e != null)
                    {
                        estado = e.Descricao ?? string.Empty;
                        uf = e.Uf ?? string.Empty;
                    }
                }

                return new CepLookupResult(cepWithHyphen, logradouro, bairro, cidade, estado, uf);
            }

            // 2) Fallback: consulta externa (ViaCEP) — implementado como opção via HttpClient.
            _logger.LogInformation("CEP {Cep} não encontrado localmente — consultando serviço externo.", cepWithHyphen);
            try
            {
                var client = _httpClientFactory.CreateClient("viaCep") ?? _httpClientFactory.CreateClient();
                // via CEP endpoint (exemplo): https://viacep.com.br/ws/{cepDigits}/json/
                var url = $"https://viacep.com.br/ws/{digits}/json/";
                using var resp = await client.GetAsync(url).ConfigureAwait(false);
                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("ViaCEP retornou {Status} para CEP {Cep}", resp.StatusCode, cepWithHyphen);
                    return null;
                }

                using var stream = await resp.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using var doc = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);

                // ViaCEP returns {"erro": true} when not found
                if (doc.RootElement.TryGetProperty("erro", out var erro) && erro.ValueKind == JsonValueKind.True)
                {
                    _logger.LogInformation("ViaCEP informou que CEP {Cep} não existe.", cepWithHyphen);
                    return null;
                }

                string logradouroExt = doc.RootElement.GetPropertyOrDefault("logradouro") ?? string.Empty;
                string bairroExt = doc.RootElement.GetPropertyOrDefault("bairro") ?? string.Empty;
                string cidadeExt = doc.RootElement.GetPropertyOrDefault("localidade") ?? string.Empty;
                string ufExt = doc.RootElement.GetPropertyOrDefault("uf") ?? string.Empty;
                string estadoExt = doc.RootElement.GetPropertyOrDefault("estado") ?? string.Empty; 

                return new CepLookupResult(cepWithHyphen, logradouroExt, bairroExt, cidadeExt, estadoExt, ufExt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao consultar serviço externo para CEP {Cep}", cepWithHyphen);
                return null;
            }
        }
    }

    internal static class JsonElementExtensions
    {
        public static string? GetPropertyOrDefault(this JsonElement root, string propertyName)
        {
            if (root.TryGetProperty(propertyName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.String)
                    return prop.GetString();
            }
            return null;
        }
    }
}
