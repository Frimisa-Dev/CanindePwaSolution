using FalcaoPwaShared.Models.Dtos.CommonDto;
using FalcaoPwaShared.Models.Dtos.UserDto;

namespace FalcaoPwaClient.Services.Offline
{
    public interface IOfflineCacheService
    {
        // ✅ CACHE DE DADOS COM CONSTRAINT DE SEGURANÇA
        Task<T?> GetCachedDataAsync<T>(string key) where T : class;
        Task SetCachedDataAsync<T>(string key, T data, TimeSpan? expiration = null) where T : class;
        Task<bool> RemoveCachedDataAsync(string key);
        Task<bool> HasCachedDataAsync(string key);

        // ✅ CONTROLE OFFLINE
        Task<bool> IsOnlineAsync();
        Task SetOnlineStatusAsync(bool isOnline);
        Task<List<OfflineOperationDto>> GetPendingOperationsAsync();
        Task AddPendingOperationAsync(OfflineOperationDto operation);
        Task ClearPendingOperationsAsync();

        // ✅ CACHE ESTRATÉGICO - MÉTODOS ESPECIALIZADOS
        Task CacheUserDataAsync(UserProfileDto user);
        Task<UserProfileDto?> GetCachedUserDataAsync();
        Task CacheAuthStateAsync(bool isAuthenticated);
        Task<bool> GetCachedAuthStateAsync();
    }
}
