namespace FalcaoPwaShared.Models.Dtos.AuthDto
{
    /// <summary>
    /// Solicitacao de recuperacao de senha
    /// Uso: ForgotPassword.razor  AuthController.ForgotPassword()
    /// </summary>
    public class ForgotPasswordDto
    {
        public required string Email { get; set; }
    }
}