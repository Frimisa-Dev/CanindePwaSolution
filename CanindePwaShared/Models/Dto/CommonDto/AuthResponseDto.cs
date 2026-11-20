/// <summary>
/// Resposta de autenticação com tokens
/// Uso: TokenService → Geração de access e refresh tokens
/// </summary>
namespace CanindePwaShared.Models.Dto.CommonDto
{
    public class AuthResponseDto
    {
        public required string RefreshToken { get; set; }
        public required DateTime ExpiresAt { get; set; }
    }
}