namespace FalcaoPwaShared.Models.Dtos.AuthDto
{
    /// <summary>
    /// Solicitacao de reenvio de PIN
    /// Uso: ResendPin.razor  AuthController.ResendPin()
    /// </summary>
    public class ResendPinDto
    {
        public required string Email { get; set; }
    }
}
