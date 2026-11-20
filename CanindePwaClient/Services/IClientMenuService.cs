using CanindePwaShared.Models.Dto.AuthDto;

namespace CanindePwaClient.Services.Menu
{
    public interface IClientMenuService
    {
        Task<IReadOnlyList<MenuItemDto>> GetMenusDoUsuarioAsync();
    }
}
