using CanindePwaShared.Models.Dto.CommonDto;

namespace CanindePwaClient.Services.Offline
{
    public interface IOfflineSyncService
    {
        Task<bool> SyncPendingOperationsAsync();
        Task<bool> TrySyncOperationAsync(OfflineOperationDto operation);
        Task ProcessPendingOperationsAsync();
        Task<bool> HasPendingOperationsAsync();
    }
}
