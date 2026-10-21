using FalcaoPwaShared.Enums;

namespace FalcaoPwaShared.Models.Dtos.AuthDto
{
    public class SeedAdminDto
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public UsuarioEnum TipoUsuario { get; set; } = UsuarioEnum.Ceo;
    }
}
