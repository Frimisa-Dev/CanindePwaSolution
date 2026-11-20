using System.Net.Http.Json;
using System.Text.Json;
using Blazored.Toast.Services;
using CanindePwaShared.Models.Dto.CommonDto;

namespace CanindePwaClient.Services.Offline
{
    public class OfflineSyncService : IOfflineSyncService
    {
        private readonly HttpClient _httpClient;
        private readonly IOfflineCacheService _offlineCache;
        private readonly IToastService _toastService;
        private bool _isSyncing = false;

        public OfflineSyncService(HttpClient httpClient, IOfflineCacheService offlineCache, IToastService toastService)
        {
            _httpClient = httpClient;
            _offlineCache = offlineCache;
            _toastService = toastService;
        }

        public async Task<bool> SyncPendingOperationsAsync()
        {
            if (_isSyncing) return false;

            _isSyncing = true;
            var success = false;

            try
            {
                var isOnline = await _offlineCache.IsOnlineAsync();
                if (!isOnline) return false;

                var pendingOps = await _offlineCache.GetPendingOperationsAsync();
                if (!pendingOps.Any()) return true;

                var successfulOps = new List<OfflineOperationDto>();

                foreach (var operation in pendingOps)
                {
                    if (await TrySyncOperationAsync(operation))
                    {
                        successfulOps.Add(operation);
                    }
                    else
                    {
                        operation.RetryCount++;
                        if (operation.RetryCount >= 3)
                        {
                            successfulOps.Add(operation);
                        }
                    }
                }

                var remainingOps = pendingOps.Except(successfulOps).ToList();
                await _offlineCache.SetCachedDataAsync("pending_operations", remainingOps);

                if (successfulOps.Any())
                {
                    _toastService.ShowSuccess($"Sincronizadas {successfulOps.Count} operações pendentes");
                    success = true;
                }
            }
            catch (Exception ex)
            {
                _toastService.ShowError("Erro ao sincronizar operações pendentes");
            }
            finally
            {
                _isSyncing = false;
            }

            return success;
        }

        public async Task<bool> TrySyncOperationAsync(OfflineOperationDto operation)
        {
            try
            {
                HttpResponseMessage response;

                switch (operation.Method.ToUpper())
                {
                    case "POST":
                        var postData = JsonSerializer.Deserialize<JsonElement>(operation.Data);
                        response = await _httpClient.PostAsJsonAsync(operation.Endpoint, postData);
                        break;
                    case "PUT":
                        var putData = JsonSerializer.Deserialize<JsonElement>(operation.Data);
                        response = await _httpClient.PutAsJsonAsync(operation.Endpoint, putData);
                        break;
                    case "DELETE":
                        response = await _httpClient.DeleteAsync(operation.Endpoint);
                        break;
                    default:
                        return false;
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task ProcessPendingOperationsAsync()
        {
            _ = await SyncPendingOperationsAsync();
        }

        public async Task<bool> HasPendingOperationsAsync()
        {
            var pendingOps = await _offlineCache.GetPendingOperationsAsync();
            return pendingOps.Any();
        }
    }
}
