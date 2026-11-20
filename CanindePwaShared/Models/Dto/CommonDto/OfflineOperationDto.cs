/// <summary>
/// Operação para sincronização offline - Áreas rurais/fazendas
/// Uso: OfflineService → Queue de operações pendentes
/// Sincronização: Quando internet estiver disponível
/// </summary>
namespace CanindePwaShared.Models.Dto.CommonDto
{
    public class OfflineOperationDto
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Endpoint { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int RetryCount { get; set; } = 0;
    }
}