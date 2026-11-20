using System.Threading.Tasks;

namespace CanindeWebServer.Services
{
    public record CepLookupResult(string Cep, string Logradouro, string Bairro, string Cidade, string Estado, string Uf);

    public interface ICepLookupService
    {
        /// <summary>
        /// Consulta o CEP em um serviço externo (Correios, ViaCEP, etc).
        /// Retorna null se não encontrado.
        /// </summary>
        Task<CepLookupResult?> LookupCepAsync(string cep);
    }
}
