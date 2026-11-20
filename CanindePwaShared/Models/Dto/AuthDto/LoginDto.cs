namespace CanindePwaShared.Models.Dto.AuthDto
{
    /// <summary>
    /// Credenciais para autenticacao
    /// Uso: Login.razor  AuthController.Login()
    /// </summary>
    public class LoginDto
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }
}