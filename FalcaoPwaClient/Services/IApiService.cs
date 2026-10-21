namespace FalcaoPwaClient.Services
{
    /// <summary>
    /// Interface unica para chamadas HTTP REST
    /// Substitui IApiService e IApiService antigos
    /// </summary>
    public interface IApiService
    {
        // Metodos GET
        Task<TResponse?> GetAsync<TResponse>(string endpoint);

        // Metodos POST com tipagem forte
        Task<TResponse?> PostAsync<TRequest, TResponse>(
            string endpoint,
            TRequest request);

        // Metodos PUT com tipagem forte
        Task<TResponse?> PutAsync<TRequest, TResponse>(
            string endpoint,
            TRequest request);

        // Metodos DELETE
        Task<bool> DeleteAsync(string endpoint);

        // Metodos utilitarios
        Task<bool> HealthCheckAsync();
        Task<DateTime?> GetServerTimeAsync();

        // Metodos especificos (opcional - pode ser generico)
        Task<bool> RequestPasswordRecoveryAsync(string email);
    }
}
