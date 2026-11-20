using System.Net.Http.Json;
using CanindePwaShared.Models.Dto.AuthDto;

namespace CanindePwaClient.Services.Menu
{
    public sealed class ClientMenuService : IClientMenuService
    {
        private readonly HttpClient _http;

        public ClientMenuService(HttpClient http)
        {
            _http = http;
        }

        public async Task<IReadOnlyList<MenuItemDto>> GetMenusDoUsuarioAsync()
        {
            var response = await _http.GetAsync("api/menu/porusuario");

            if (!response.IsSuccessStatusCode)
                return Array.Empty<MenuItemDto>();

            var menus = await response.Content
                .ReadFromJsonAsync<IReadOnlyList<MenuItemDto>>();

            return menus ?? Array.Empty<MenuItemDto>();
        }
    }
}
