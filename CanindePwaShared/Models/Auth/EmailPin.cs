/// <summary>
/// PIN de segurança para autenticação/confirmação de email
/// Uso: Envio de PIN por email, validação de novo usuário
/// Regra: Usuário recebe PIN por email para ativação da conta
/// </summary>
namespace CanindePwaShared.Models.Auth
{
    public class EmailPin
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Code { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public bool Used { get; set; } = false;
        public User User { get; set; } = null!;
    }
}