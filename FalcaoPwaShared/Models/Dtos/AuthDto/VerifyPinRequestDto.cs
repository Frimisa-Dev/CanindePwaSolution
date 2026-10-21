/// <summary>
/// Resposta da validação do PIN
/// Uso: AuthController.VerifyPin() → Frontend
/// Regra: Se PIN válido, retorna token de acesso
/// </summary>
namespace FalcaoPwaShared.Models.Dtos.AuthDto
{
    public class VerifyPinResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }
}