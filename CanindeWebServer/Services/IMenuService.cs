using CanindePwaShared.Models.Dto.AuthDto;

namespace CanindeWebServer.Services
{
    /// <summary>
    /// Contrato oficial para resolução de menus.
    /// Fonte de verdade para MenuController.
    /// </summary>
    public interface IMenuService
    {
        Task<IReadOnlyList<MenuItemDto>> GetMenusPorUsuarioAsync(int userId);

        Task<IReadOnlyList<MenuItemDto>> GetMenusPorPerfilAsync(string perfilCodigo);
    }
}
