namespace Client.Core.Interfaces.DTO
{
    public class RecordDataCollection
    {
        public DateTime? Time { get; set; }
        public uint? DistanceMeters { get; set; }
        public uint? SpeedMetersPerHour { get; set; }
        public byte? HeartRate { get; set; }
        public ushort? Altitude { get; set; }
        public int? Latitude { get; set; }
        public int? Longitude { get; set; }

        public int? Power { get; set; }
        public byte? Cadence { get; set; }
    }
}
