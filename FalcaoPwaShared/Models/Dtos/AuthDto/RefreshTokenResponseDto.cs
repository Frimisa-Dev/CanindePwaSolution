namespace FalcaoPwaShared.Models.Dtos.AuthDto
{
    /// <summary>
    /// Resposta do refresh token
    /// OBS: Refresh token e cookie HTTPOnly
    /// </summary>
    public class RefreshTokenResponseDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        // Access token e cookie HTTPOnly - nao enviar no JSON
    }
}
