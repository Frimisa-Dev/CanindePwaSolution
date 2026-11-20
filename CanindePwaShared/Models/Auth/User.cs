/// <summary>
/// Entidade principal de usuário - Segurança Nível Bancário
/// Uso: ProjetoDbContext, AuthManager, UserService
/// </summary>
namespace CanindePwaShared.Models.Auth
{
    public class User
    {
        public int Id { get; set; }
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string PasswordHash { get; set; } // ✅ SHA256 apenas
        public bool IsLocked { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public required string TipoUsuario { get; set; } // ✅ String no domínio também
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

        // ✅ SEGURANÇA: Removido TempPassword - senhas temporárias devem ser hasheadas
        // ✅ SEGURANÇA: RefreshTokens como coleção separada
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}