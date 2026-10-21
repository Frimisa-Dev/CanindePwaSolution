using System.Text.Json;
using FalcaoPwaShared.Models.Dtos.UserDto;
using FalcaoPwaShared.Models.Dtos.CommonDto;
using Microsoft.JSInterop;

namespace FalcaoPwaClient.Services.Offline
{
    public class OfflineCacheService : IOfflineCacheService
    {
        private readonly IJSRuntime _jsRuntime;
        private const string CachePrefix = "curio_offline_";
        private const string PendingOpsKey = "pending_operations";
        private const string UserDataKey = "cached_user_data";
        private const string AuthStateKey = "cached_auth_state";
        private const string NetworkStatusKey = "network_status";

        public OfflineCacheService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        // ✅ IMPLEMENTAÇÃO SEGURA COM CONSTRAINT
        public async Task<T?> GetCachedDataAsync<T>(string key) where T : class
        {
            try
            {
                var fullKey = $"{CachePrefix}{key}";
                var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", fullKey);

                if (string.IsNullOrEmpty(json))
                    return default;

                var cachedItem = JsonSerializer.Deserialize<CachedItemDto<T>>(json);

                if (cachedItem?.IsExpired == true)
                {
                    await RemoveCachedDataAsync(key);
                    return default;
                }

                return cachedItem?.Data;
            }
            catch
            {
                return default;
            }
        }

        public async Task SetCachedDataAsync<T>(string key, T data, TimeSpan? expiration = null) where T : class
        {
            try
            {
                var fullKey = $"{CachePrefix}{key}";
                var cachedItem = new CachedItemDto<T>
                {
                    Data = data,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.Add(expiration ?? TimeSpan.FromHours(24))
                };

                var json = JsonSerializer.Serialize(cachedItem);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", fullKey, json);
            }
            catch
            {
                // Falha silenciosa em modo offline
            }
        }

        public async Task<bool> RemoveCachedDataAsync(string key)
        {
            try
            {
                var fullKey = $"{CachePrefix}{key}";
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", fullKey);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> HasCachedDataAsync(string key)
        {
            try
            {
                var fullKey = $"{CachePrefix}{key}";
                var item = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", fullKey);
                return !string.IsNullOrEmpty(item);
            }
            catch
            {
                return false;
            }
        }

        // ✅ CONTROLE DE STATUS ONLINE/OFFLINE
        public async Task<bool> IsOnlineAsync()
        {
            try
            {
                return await _jsRuntime.InvokeAsync<bool>("navigator.onLine");
            }
            catch
            {
                return true;
            }
        }

        public async Task SetOnlineStatusAsync(bool isOnline)
        {
            await SetCachedDataAsync(NetworkStatusKey, new BoolWrapperDto(isOnline));
        }

        // ✅ OPERAÇÕES PENDENTES
        public async Task<List<OfflineOperationDto>> GetPendingOperationsAsync()
        {
            return await GetCachedDataAsync<List<OfflineOperationDto>>(PendingOpsKey) ?? new List<OfflineOperationDto>();
        }

        public async Task AddPendingOperationAsync(OfflineOperationDto operation)
        {
            var operations = await GetPendingOperationsAsync();
            operations.Add(operation);
            await SetCachedDataAsync(PendingOpsKey, operations);
        }

        public async Task ClearPendingOperationsAsync()
        {
            await RemoveCachedDataAsync(PendingOpsKey);
        }

        // ✅ CACHE ESTRATÉGICO - DADOS DO USUÁRIO
        public async Task CacheUserDataAsync(UserProfileDto user)
        {
            await SetCachedDataAsync(UserDataKey, user, TimeSpan.FromDays(7));
        }

        public async Task<UserProfileDto?> GetCachedUserDataAsync()
        {
            return await GetCachedDataAsync<UserProfileDto>(UserDataKey);
        }

        // ✅ MÉTODOS ESPECIALIZADOS PARA VALUE TYPES
        public async Task CacheAuthStateAsync(bool isAuthenticated)
        {
            await SetCachedDataAsync(AuthStateKey, new BoolWrapperDto(isAuthenticated), TimeSpan.FromHours(2));
        }

        public async Task<bool> GetCachedAuthStateAsync()
        {
            var wrapper = await GetCachedDataAsync<BoolWrapperDto>(AuthStateKey);
            return wrapper ?? false;
        }
    }
}
