using FalcaoPwaShared.Models.Dtos.AuthDto;
using FalcaoWebServer.Data;
using Microsoft.EntityFrameworkCore;

namespace FalcaoWebServer.Services
{
    /// <summary>
    /// Serviço responsável por RESOLVER menus com base
    /// em identidade, perfil e regras de negócio.
    /// </summary>
    public sealed class MenuService : IMenuService
    {
        private readonly FalcaoDbContext _context;

        public MenuService(FalcaoDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Resolve menus para o usuário autenticado.
        /// Fonte de verdade usada pelo frontend.
        /// </summary>
        public async Task<IReadOnlyList<MenuItemDto>> GetMenusPorUsuarioAsync(int userId)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

            if (user == null)
                return Array.Empty<MenuItemDto>();

            return await GetMenusPorPerfilAsync(
                MapTipoUsuarioParaPerfil(user.TipoUsuario)
            );
        }

        /// <summary>
        /// Resolve menus por perfil (TipoUsuario).
        /// </summary>
        public async Task<IReadOnlyList<MenuItemDto>> GetMenusPorPerfilAsync(string perfilCodigo)
        {
            return await _context.Menus
                .AsNoTracking()
                .Include(m => m.Rota)
                .Include(m => m.Perfil)
                .Where(m =>
                    m.Perfil.Codigo == perfilCodigo &&
                    m.Ativo &&
                    m.Rota.Ativo)
                .OrderBy(m => m.Ordem)
                .Select(m => new MenuItemDto
                {
                    RotaId = m.RotaId,
                    Nome = m.Rota.Nome,
                    Caminho = m.Rota.Caminho,
                    Icone = m.Rota.Icone,
                    Ordem = m.Ordem ?? 0,
                    Ativo = m.Ativo
                })
                .ToListAsync();
        }

        private static string MapTipoUsuarioParaPerfil(string tipoUsuario) =>
            tipoUsuario switch
        {
            "Ceo" => "CEO",
            "Administrador" => "ADMIN",
            "Vendedor" => "VENDEDOR",
            "Operador" => "OPERADOR",
            "Cliente" => "CLIENTE",
            _ => "CLIENTE"
        };
    }
}
