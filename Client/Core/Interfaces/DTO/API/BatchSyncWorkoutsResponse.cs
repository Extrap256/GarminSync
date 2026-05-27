namespace Client.Core.Interfaces.DTO.API
{
    public class BatchSyncWorkoutsResponse
    {
        public int Synced {  get; set; }
        public int Failed { get; set; }
        public List<int> SyncedIds { get; set; } = new();
        public List<BatchSyncError> Errors { get; set; } = new();
    }
}
