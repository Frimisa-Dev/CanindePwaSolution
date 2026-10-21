using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Text.Json;
using FalcaoPwaShared.Models.Dtos.UserDto;
using System.Net;

namespace FalcaoPwaClient.Auth
{
    public class CookieAuthStateProvider : AuthenticationStateProvider
    {
        private readonly HttpClient _httpClient;
        private readonly UserContext _userContext;

        private const int SERVER_VALIDATION_MINUTES = 5;

        private AuthenticationState _cachedState = CreateAnonymous();
        private DateTime? _lastServerValidation;
        private bool _initialized;

        public CookieAuthStateProvider(IHttpClientFactory httpClientFactory, UserContext userContext)
        {
            _httpClient = httpClientFactory.CreateClient("FalcaoApi");
            _userContext = userContext;
        }

        // =====================================================
        // 🔹 REGRA: ESTE MÉTODO APENAS LÊ ESTADO
        // =====================================================
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            if (!_initialized)
            {
                _initialized = true;
                _cachedState = await ValidateWithServerAsync(force: true);
                return _cachedState;
            }

            if (ShouldValidateWithServer())
            {
                _cachedState = await ValidateWithServerAsync(force: false);
            }

            return _cachedState;
        }

        private bool ShouldValidateWithServer()
        {
            if (!_lastServerValidation.HasValue)
                return true;

            return (DateTime.UtcNow - _lastServerValidation.Value)
                .TotalMinutes >= SERVER_VALIDATION_MINUTES;
        }

        // =====================================================
        // 🔹 REGRA: VALIDA, MAS NÃO NOTIFICA
        // =====================================================
        private async Task<AuthenticationState> ValidateWithServerAsync(bool force)
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/auth/profile");

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _userContext.Clear();
                    ResetAuthentication();
                    return _cachedState;
                }

                if (!response.IsSuccessStatusCode)
                {
                    if (!force)
                        return _cachedState;

                    ResetInternal();
                    return _cachedState;
                }

                var json = await response.Content.ReadAsStringAsync();

                var profile = JsonSerializer.Deserialize<UserProfileResponseDto>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (profile?.User == null)
                {
                    ResetInternal();
                    return _cachedState;
                }

                // Estado 2 - Usuário pendente (PendingPin)
                if (!profile.User.IsActive)
                {
                    _cachedState = CreateAuthenticated(profile.User);
                    _lastServerValidation = DateTime.UtcNow;

                    // ⚠️ NÃO retorna 401
                    // ⚠️ NÃO reseta autenticação
                    // O bloqueio será feito pelas Policies
                    return _cachedState;
                }

                _cachedState = CreateAuthenticated(profile.User);
                _lastServerValidation = DateTime.UtcNow;

                return _cachedState;
            }
            catch (HttpRequestException ex)
            {
                if (!force)
                    return _cachedState;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception geral: {ex.Message}");
            }

            ResetInternal();
            return _cachedState;
        }

        // =====================================================
        // 🔹 CRIA USUÁRIO AUTENTICADO
        // =====================================================
        private static AuthenticationState CreateAuthenticated(UserProfileDto user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("TipoUsuario", user.TipoUsuario),
                new Claim("IsActive", user.IsActive.ToString())
            };

            if (user.Roles != null)
            {
                foreach (var role in user.Roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
            }

            var identity = new ClaimsIdentity(claims, "CookieAuth");
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }

        private static AuthenticationState CreateAnonymous()
        {
            return new AuthenticationState(
                new ClaimsPrincipal(new ClaimsIdentity())
            );
        }

        // =====================================================
        // 🔹 RESET INTERNO (SEM EVENTO)
        // =====================================================
        private void ResetInternal()
        {
            _cachedState = CreateAnonymous();
            _lastServerValidation = null;
        }

        // =====================================================
        // 🔔 API PÚBLICA (AQUI PODE NOTIFICAR)
        // =====================================================

        public async Task ForceServerValidationAsync()
        {
            _lastServerValidation = DateTime.MinValue;
            _cachedState = await ValidateWithServerAsync(force: true);
            NotifyAuthenticationStateChanged(Task.FromResult(_cachedState));
        }

        public void ResetAuthentication()
        {
            _initialized = false;
            ResetInternal();
            NotifyAuthenticationStateChanged(Task.FromResult(_cachedState));
        }
    }
}

