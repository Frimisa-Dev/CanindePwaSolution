/// <summary>
/// Resposta padronizada para operações de perfil
/// Uso: AuthController.Profile() → Frontend
/// </summary>
namespace FalcaoPwaShared.Models.Dtos.UserDto
{
    public class UserProfileResponseDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public UserProfileDto? User { get; set; }
    }
}