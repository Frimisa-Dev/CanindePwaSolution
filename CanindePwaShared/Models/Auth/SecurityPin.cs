/// <summary>
/// PIN de segurança para autenticação em duas etapas
/// Uso: Verificação de email, operações sensíveis
/// </summary>

namespace CanindePwaShared.Models.Auth
{
    public class SecurityPin
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public required string CodeHash { get; set; } // ✅ Hash do PIN
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; } = false;
        public required string Purpose { get; set; } // "EmailVerification", "PasswordReset"
        public required User User { get; set; }

        public bool IsValid => !IsUsed && DateTime.UtcNow < ExpiresAt;
    }
}