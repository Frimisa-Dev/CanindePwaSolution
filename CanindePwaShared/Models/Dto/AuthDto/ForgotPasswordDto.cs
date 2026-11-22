using System.ComponentModel.DataAnnotations;

namespace CanindePwaShared.Models.Dto.AuthDto
{
    /// <summary>
    /// Solicitacao de recuperacao de senha (passo 1: pedir o link por e-mail)
    /// Uso: RecuperarAcesso.razor -> AuthController.ForgotPassword()
    /// </summary>
    public class ForgotPasswordDto
    {
        [Required]
        [EmailAddress]
        [StringLength(254)]
        public required string Email { get; set; }
    }
}
