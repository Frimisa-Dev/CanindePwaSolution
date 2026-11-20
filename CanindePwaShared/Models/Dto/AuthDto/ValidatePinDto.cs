namespace CanindePwaShared.Models.Dto.AuthDto
{
    /// <summary>
    /// Homologação do novo usuário (POST /api/auth/validatepin):
    /// PIN recebido por e-mail + senha definitiva. A conta só é ativada se os dois estiverem corretos.
    /// </summary>
    public class ValidatePinDto
    {
        public required string Code { get; set; }
        public required string NewPassword { get; set; }
        public required string ConfirmPassword { get; set; }
    }
}
