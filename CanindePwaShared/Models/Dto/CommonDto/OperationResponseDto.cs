/// <summary>
/// Resposta genérica para operações simples
/// Uso: Qualquer endpoint que retorne apenas success/message
/// </summary>
namespace CanindePwaShared.Models.Dto.CommonDto
{
    public class OperationResponseDto
    {
        public bool Success { get; set; }
        public required string Message { get; set; }
    }
}