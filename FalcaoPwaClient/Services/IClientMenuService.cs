using FalcaoPwaShared.Models.Dtos.AuthDto;

namespace FalcaoPwaClient.Services.Menu
{
    public interface IClientMenuService
    {
        Task<IReadOnlyList<MenuItemDto>> GetMenusDoUsuarioAsync();
    }
}
