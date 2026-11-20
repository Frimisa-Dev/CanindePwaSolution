namespace CanindePwaClient.Services
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

        // POST que devolve tambem o status HTTP e o corpo da resposta MESMO quando o servidor
        // responde erro (400, 401...). Usado quando a tela precisa dos detalhes do erro
        // (ex.: tentativas restantes do PIN, regras de senha violadas).
        Task<ApiCallResultService<TResponse>> PostWithStatusAsync<TRequest, TResponse>(
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
