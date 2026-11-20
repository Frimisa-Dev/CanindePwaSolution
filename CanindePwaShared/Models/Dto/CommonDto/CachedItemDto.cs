/// <summary>
/// Item em cache com expiração - Segurança Nível Bancário
/// Uso: CacheService, OfflineService
/// </summary>
namespace CanindePwaShared.Models.Dto.CommonDto
{
    public class CachedItemDto<T> where T : class
    {
        public T? Data { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    }
}