using FalcaoPwaClient.Services;
using FalcaoPwaShared.Models.Dtos.AuthDto;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace FalcaoPwaClient.Auth
{
    public class AuthManager
    {
        private readonly AuthenticationStateProvider _authStateProvider;
        private readonly NavigationManager _navigation;
        private readonly IApiService _apiService;
        private readonly UserContext _userContext;

        public AuthManager(
            AuthenticationStateProvider authStateProvider,
            NavigationManager navigation,
            IApiService apiService,
            UserContext userContext)
        {
            _authStateProvider = authStateProvider;
            _navigation = navigation;
            _apiService = apiService;
            _userContext = userContext;
        }

        // ============================
        // LOGIN
        // ============================
        public async Task<LoginResponseDto> LoginAsync(LoginDto dto)
        {
            var response = await _apiService.PostAsync<LoginDto, LoginResponseDto>(
                "api/auth/login", dto);

            if (response?.Success == true && !response.RequiresPin)
            {
                await RevalidateAndReloadAsync();
            }

            return response ?? new LoginResponseDto
            {
                Success = false,
                Message = "Erro de conexão"
            };
        }

        // ============================
        // CADASTRO
        // ============================
        public async Task<CadastroUsuarioResponseDto> CadastroUsuarioAsync(CadastroUsuarioDto dto)
        {
            try
            {
                var response = await _apiService.PostAsync<CadastroUsuarioDto, CadastroUsuarioResponseDto>(
                    "api/auth/cadastrarusuario", dto);

                if (response != null)
                {
                    return response;
                }

                return new CadastroUsuarioResponseDto
                {
                    Success = false,
                    Message = "Resposta vazia do servidor"
                };
            }
            catch (Exception ex)
            {
                return new CadastroUsuarioResponseDto
                {
                    Success = false,
                    Message = "Erro de conexão com o servidor"
                };
            }
        }

        // ============================
        // PIN
        // ============================
        public async Task<bool> ValidatePinAsync(string pin)
        {
            var response = await _apiService.PostAsync<ValidatePinDto, LoginResponseDto>(
                "api/auth/validatepin",
                new ValidatePinDto { Code = pin });

            if (response?.Success == true)
            {
                await RevalidateAndReloadAsync();
                return true;
            }

            return false;
        }

        public async Task<bool> ResendPinAsync(string? email = null)
        {
            var dto = new ResendPinDto { Email = email ?? string.Empty };

            var response = await _apiService.PostAsync<ResendPinDto, RefreshTokenResponseDto>(
                "api/auth/resendpin", dto);

            return response?.Success ?? false;
        }

        // ============================
        // LOGOUT
        // ============================
        public async Task LogoutAsync()
        {
            try
            {
                await _apiService.PostAsync<object, object>("api/auth/logout", new { });
            }
            catch
            {
                // mesmo se falhar, seguimos com limpeza local
            }

            if (_authStateProvider is CookieAuthStateProvider provider)
                provider.ResetAuthentication();

            _userContext.Clear();

            _navigation.NavigateTo("/", true);
        }

        // ============================
        // UTIL
        // ============================
        private async Task RevalidateAndReloadAsync()
        {
            if (_authStateProvider is CookieAuthStateProvider provider)
                await provider.ForceServerValidationAsync();

            await _userContext.LoadAsync();
        }
    }
}