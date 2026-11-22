namespace CanindePwaShared.Models.Dto.AuthDto
{
    /// <summary>
    /// Resposta do POST api/auth/resetpassword (sucesso e erro 400).
    /// Uso: RedefinirSenha.razor
    /// </summary>
    public class ResetPasswordResponseDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }

        /// <summary>Regras da senha violadas (o token continua valido para nova tentativa).</summary>
        public List<string>? Errors { get; set; }

        /// <summary>true = link invalido, expirado ou ja usado: e preciso pedir outro.</summary>
        public bool InvalidToken { get; set; }
    }
}
