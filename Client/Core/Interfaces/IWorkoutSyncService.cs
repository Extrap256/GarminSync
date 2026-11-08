using Client.Core.Interfaces.DTO.API;

namespace Client.Core.Interfaces
{
    public interface IWorkoutSyncService
    {
        /// <summary>
        /// Синхронизировать одну тренировку с сервером
        /// </summary>
        Task<SyncWorkoutResponse> SyncWorkoutAsync(SyncWorkoutRequest request, CancellationToken ct = default);
        
        /// <summary>
        /// Массовая синхронизация всех несинхронизированных тренировок.
        /// Разбивает на чанки по 50 и отправляет батчами.
        /// Возвращает сводку результатов.
        /// </summary>
        Task<BatchSyncResult> SyncAllUnsyncedBatchAsync(CancellationToken ct = default);
    }
}
