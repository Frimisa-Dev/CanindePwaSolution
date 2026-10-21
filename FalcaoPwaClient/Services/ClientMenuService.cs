using System.Net.Http.Json;
using FalcaoPwaShared.Models.Dtos.AuthDto;

namespace FalcaoPwaClient.Services.Menu
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
