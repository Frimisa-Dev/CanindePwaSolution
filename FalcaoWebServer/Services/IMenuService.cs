using FalcaoPwaShared.Models.Dtos.AuthDto;

namespace FalcaoWebServer.Services
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
