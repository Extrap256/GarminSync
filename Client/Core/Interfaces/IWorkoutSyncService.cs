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
        /// Синхронизировать все несинхронизированные тренировки
        /// </summary>
        Task<int> SyncAllUnsyncedAsync(CancellationToken ct = default);
    }
}
