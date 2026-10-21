using FalcaoPwaShared.Models.Dtos.UserDto;
using FalcaoPwaShared.Models.Dtos.AuthDto;
using FalcaoPwaClient.Services;

namespace FalcaoPwaClient.Auth
{
    public class UserContext
    {
        private readonly IApiService _apiService;

        public UserProfileDto? CurrentUser { get; private set; }
        public IReadOnlyList<MenuItemDto>? Menus { get; private set; }

        public bool IsLoaded { get; private set; }
        public bool IsAuthenticated => CurrentUser != null;

        public event Action? OnChange;

        public UserContext(IApiService apiService)
        {
            _apiService = apiService;
        }

        // ============================
        // CARGA CENTRALIZADA
        // ============================
        public async Task LoadAsync()
        {
            try
            {
                var profileResponse =
                    await _apiService.GetAsync<UserProfileResponseDto>("api/auth/profile");

                CurrentUser = profileResponse?.User;

                if (CurrentUser != null)
                {
                    Menus = await _apiService
                        .GetAsync<IReadOnlyList<MenuItemDto>>("api/menu/porusuario");
                }
                else
                {
                    Menus = null;
                }
            }
            catch
            {
                CurrentUser = null;
                Menus = null;
            }
            finally
            {
                IsLoaded = true;
                OnChange?.Invoke();
            }
        }

        // ============================
        // RESET (LOGOUT / EXPIRAÇÃO)
        // ============================
        public void Clear()
        {
            CurrentUser = null;
            Menus = null;
            IsLoaded = false;
            OnChange?.Invoke();
        }
    }
}
