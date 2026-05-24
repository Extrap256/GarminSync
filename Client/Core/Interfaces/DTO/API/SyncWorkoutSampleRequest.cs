namespace Client.Core.Interfaces.DTO.API
{
    public class SyncWorkoutSampleRequest
    {
        public uint? TimeSeconds { get; set; }
        public uint? DistanceMeters { get; set; }
        public uint? SpeedMetersPerHour { get; set; }
        public byte? HeartRate { get; set; }
        public ushort? Altitude { get; set; }
        public int? Latitude { get; set; }
        public int? Longitude { get; set; }
        public string Discriminator { get; set; } = null!;

    }
}
