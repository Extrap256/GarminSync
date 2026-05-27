namespace Client.Core.Interfaces.DTO.API
{
    public class BatchSyncError
    {
        public string Identifier { get; set; } = null!;
        public string Error { get; set; } = null!;
    }
}
