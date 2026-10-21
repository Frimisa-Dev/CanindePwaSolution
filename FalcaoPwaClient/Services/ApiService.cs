using FalcaoPwaShared.Models.Dtos.AuthDto;
using System.Net.Http.Json;

namespace FalcaoPwaClient.Services
{
    /// <summary>
    /// Implementacao unificada para chamadas HTTP REST
    /// Substitui ApiService e ApiService antigos
    /// </summary>
    public class ApiService : IApiService
    {
        private readonly HttpClient _http;

        public ApiService(HttpClient http)
        {
            _http = http;
        }

        // ======================
        // METODOS GET
        // ======================
        public async Task<TResponse?> GetAsync<TResponse>(string endpoint)
        {
            try
            {
                return await _http.GetFromJsonAsync<TResponse>(endpoint);
            }
            catch
            {
                return default;
            }
        }

        // ======================
        // METODOS POST
        // ======================
        public async Task<TResponse?> PostAsync<TRequest, TResponse>(
            string endpoint,
            TRequest request)
        {
            try
            {
                var response = await _http.PostAsJsonAsync(endpoint, request);

                if (!response.IsSuccessStatusCode)
                    return default;

                return await response.Content.ReadFromJsonAsync<TResponse>();
            }
            catch
            {
                return default;
            }
        }

        // ======================
        // METODOS PUT
        // ======================
        public async Task<TResponse?> PutAsync<TRequest, TResponse>(
            string endpoint,
            TRequest request)
        {
            try
            {
                var response = await _http.PutAsJsonAsync(endpoint, request);

                if (!response.IsSuccessStatusCode)
                    return default;

                return await response.Content.ReadFromJsonAsync<TResponse>();
            }
            catch
            {
                return default;
            }
        }

        // ======================
        // METODOS DELETE
        // ======================
        public async Task<bool> DeleteAsync(string endpoint)
        {
            try
            {
                var response = await _http.DeleteAsync(endpoint);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // ======================
        // METODOS UTILITARIOS
        // ======================
        public async Task<bool> HealthCheckAsync()
        {
            try
            {
                var result = await _http.GetAsync("/health");
                return result.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<DateTime?> GetServerTimeAsync()
        {
            return await GetAsync<DateTime?>("/time");
        }

        // ======================
        // METODOS ESPECIFICOS
        // ======================
        public async Task<bool> RequestPasswordRecoveryAsync(string email)
        {
            try
            {
                var payload = new { email = email.Trim() };
                return await PostAsync<object, object>("api/auth/forgot-password", payload) != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
