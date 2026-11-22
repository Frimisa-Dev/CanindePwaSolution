using CanindePwaShared.Models.Dto.AuthDto;
using System.Net.Http.Json;

namespace CanindePwaClient.Services
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
        // POST COM STATUS E CORPO DE ERRO
        // ======================
        // Diferente do PostAsync (que devolve default em qualquer falha), este devolve o status HTTP
        // e tenta ler o corpo JSON tambem nas respostas de erro. Falha de rede = StatusCode 0.
        public async Task<ApiCallResultService<TResponse>> PostWithStatusAsync<TRequest, TResponse>(
            string endpoint,
            TRequest request)
        {
            try
            {
                var response = await _http.PostAsJsonAsync(endpoint, request);

                TResponse? body = default;
                try
                {
                    body = await response.Content.ReadFromJsonAsync<TResponse>();
                }
                catch
                {
                    // corpo vazio ou que nao e JSON: segue so com o status
                }

                return new ApiCallResultService<TResponse>
                {
                    IsSuccess = response.IsSuccessStatusCode,
                    StatusCode = (int)response.StatusCode,
                    Body = body
                };
            }
            catch
            {
                return new ApiCallResultService<TResponse> { IsSuccess = false, StatusCode = 0 };
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
        // Passo 1 da recuperacao de senha. A rota do servidor e "forgotpassword" (sem hifen; antes
        // o cliente chamava "forgot-password", dava 404 e a tela sempre dizia "erro no envio").
        // Devolve status e corpo para a tela separar "limite de pedidos" (429) de "sem conexao" (0).
        public Task<ApiCallResultService<object>> RequestPasswordRecoveryAsync(string email)
        {
            return PostWithStatusAsync<ForgotPasswordDto, object>(
                "api/auth/forgotpassword",
                new ForgotPasswordDto { Email = email.Trim() });
        }

        // Passo 2: troca a senha com o token do link. Status e corpo voltam tambem em 400.
        public Task<ApiCallResultService<ResetPasswordResponseDto>> ResetPasswordAsync(ResetPasswordDto dto)
        {
            return PostWithStatusAsync<ResetPasswordDto, ResetPasswordResponseDto>(
                "api/auth/resetpassword", dto);
        }
    }
}
