using Server.API.Models.Workout;

namespace Server.Core.Interfaces
{
    public interface IWorkoutSyncService
    {
        Task<int> SyncWorkoutAsync(int userId, WorkoutDto workoutDto, CancellationToken ct = default);
        Task<WorkoutBatchSyncResponse> SyncWorkoutsBatchAsync(int userId, List<WorkoutDto> dtos, CancellationToken ct = default);

        Task<bool> IsWorkoutExistAsync(string identifier, CancellationToken ct = default);
    }
}
