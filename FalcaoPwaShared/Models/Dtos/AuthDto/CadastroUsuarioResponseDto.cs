/// <summary>
/// Resposta do processo de registro
/// Uso: AuthController.Cadastrousuario() → Frontend
/// Regra: Confirma cadastro e informa que PIN foi enviado
/// </summary>
namespace FalcaoPwaShared.Models.Dtos.AuthDto
{
    public class CadastroUsuarioResponseDto
    {
        public required string Message { get; set; }
        public bool Success { get; set; }
        public int UserId { get; set; }
        public bool RequiresPin { get; set; }
    }
}