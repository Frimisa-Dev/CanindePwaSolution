using CanindePwaShared.Models.Dto.AuthDto;
using CanindeWebServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CanindeWebServer.Controllers
{
    [ApiController]
    [Route("api/menu")]
    [Authorize]
    public sealed class MenuController : ControllerBase
    {
        private readonly IMenuService _menuService;

        public MenuController(IMenuService menuService)
        {
            _menuService = menuService;
        }

        /// <summary>
        /// Endpoint único e oficial de menu do usuário autenticado.
        /// Fonte de verdade para o frontend.
        /// </summary>
        [HttpGet("porusuario")]
        public async Task<ActionResult<IReadOnlyList<MenuItemDto>>> GetMenuPorUsuario()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var menus = await _menuService.GetMenusPorUsuarioAsync(userId);
            return Ok(menus);
        }

        [HttpGet("health")]
        [AllowAnonymous]
        public IActionResult Health()
        {
            return Ok(new
            {
                status = "Menu API OK",
                utc = DateTime.UtcNow
            });
        }
    }
}