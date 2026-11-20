/// <summary>
/// Perfil completo do usuário autenticado
/// Uso: UserProfile.razor, Dashboard.razor, AuthController.Profile()
/// </summary>
namespace CanindePwaShared.Models.Dto.UserDto
{
    public class UserProfileDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string[] Roles { get; set; } = Array.Empty<string>();
        public string TipoUsuario { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; } = true;
        public List<string> Permissions { get; set; } = new List<string>();
    }
}