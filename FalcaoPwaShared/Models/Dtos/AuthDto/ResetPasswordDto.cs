namespace FalcaoPwaShared.Models.Dtos.AuthDto
{
    /// <summary>
    /// Redefinicao de senha
    /// Uso: ResetPassword.razor  AuthController.ResetPassword()
    /// </summary>
    public class ResetPasswordDto
    {
        public required string Email { get; set; }
        public required string Token { get; set; } // Token do email
        public required string NewPassword { get; set; }
        public required string ConfirmPassword { get; set; }
    }
}
