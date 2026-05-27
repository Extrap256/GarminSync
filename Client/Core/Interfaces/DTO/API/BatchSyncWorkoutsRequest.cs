namespace Client.Core.Interfaces.DTO.API
{
    public class BatchSyncWorkoutsRequest
    {
        public List<SyncWorkoutRequest> Workouts { get; set; } = new();
    }
}
