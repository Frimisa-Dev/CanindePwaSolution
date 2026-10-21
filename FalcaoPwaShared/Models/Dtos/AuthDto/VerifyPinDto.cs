/// <summary>
/// Validação de PIN recebido por email
/// Uso: PinVerification.razor → AuthController.VerifyPin()
/// Regra: Usuário digita PIN recebido para ativar conta
/// </summary>
namespace FalcaoPwaShared.Models.Dtos.AuthDto
{
    public class VerifyPinDto
    {
        public required string Email { get; set; }
        public required string Code { get; set; }
    }
}