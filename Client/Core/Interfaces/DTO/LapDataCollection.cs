namespace Client.Core.Interfaces.DTO
{
    public class LapDataCollection
    {
        public DateTime? TimeEndUtc { get; set; }
        public uint? DistanceMeters { get; set; }
        public uint? AvgSpeedMetersPerHour { get; set; }
        public byte? AvgHeartRate { get; set; }
        public ushort? ElevationGain { get; set; }

        public byte? AvgCadence { get; set; }
        public int? AvgPower { get; set; }
    }
}
