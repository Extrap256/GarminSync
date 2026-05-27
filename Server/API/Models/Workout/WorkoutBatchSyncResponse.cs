namespace Server.API.Models.Workout
{
    public class WorkoutBatchSyncResponse
    {
        public int Synced {  get; set; }
        public int Failed { get; set; }
        public List<int> SyncedIds { get; set; } = new();
        public List<BatchSyncError> Errors { get; set; } = new();
    }
}
