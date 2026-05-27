namespace Server.API.Models.Workout
{
    public class BatchSyncError
    {
        public string Identifer {  get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }
}
