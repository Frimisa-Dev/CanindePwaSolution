using FalcaoPwaShared.Models.Dtos.CommonDto;

namespace FalcaoPwaClient.Services.Offline
{
    public interface IOfflineSyncService
    {
        Task<bool> SyncPendingOperationsAsync();
        Task<bool> TrySyncOperationAsync(OfflineOperationDto operation);
        Task ProcessPendingOperationsAsync();
        Task<bool> HasPendingOperationsAsync();
    }
}
