using FalcaoPwaShared.Models.Dtos.UserDto;

namespace FalcaoPwaShared.Models.Dtos.AuthDto
{
    /// <summary>
    /// Resposta do processo de login
    /// IMPORTANTE: Tokens sao cookies HTTPOnly
    /// </summary>
    public class LoginResponseDto
    {
        public bool Success { get; set; }
        public required string Message { get; set; }
        public bool RequiresPin { get; set; }
        public UserProfileDto? User { get; set; }
        // Tokens sao cookies HTTPOnly - nao enviar no JSON
    }
}