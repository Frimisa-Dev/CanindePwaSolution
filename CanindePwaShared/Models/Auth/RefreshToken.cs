/// <summary>
/// Token de refresh com rastreabilidade completa
/// Uso: Rotação segura de tokens, revogação, auditoria
/// </summary>
namespace CanindePwaShared.Models.Auth
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public required string TokenHash { get; set; } // ✅ Apenas hash armazenado
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RevokedAt { get; set; }
        public required string CreatedByIp { get; set; }
        public string Purpose { get; set; } = "Authentication";
        public string? ReplacedByTokenHash { get; set; }
        public required User User { get; set; }

        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public bool IsActive => RevokedAt == null && !IsExpired;
    }
}