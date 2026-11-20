/// <summary>
/// Senha temporária para recuperação de acesso
/// Uso: Sistema de recuperação de senha por email
/// ATENÇÃO: Senha em texto puro apenas para envio por email, NÃO armazenar assim
/// </summary>

namespace CanindePwaShared.Models.Auth
{ 
    public class TempPassword
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string PasswordPlain { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public bool Used { get; set; } = false;
        public User User { get; set; } = null!;
    }
}