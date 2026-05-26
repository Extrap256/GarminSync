namespace Client.Core.Interfaces.DTO
{
    public class FitDataCollector
    {
        public enum SkiStyle
        {
            Skate,
            Classic,
            DoublePoling
        }
        public int OffsetInSeconds {  get; set; }
        public DeviceData DeviceData { get; set; } = new();
        public DateTime? TimeStartUtc { get; set; }
        public DateTime? TimeEndUtc { get; set; }
        public uint? DistanceMeters { get; set; }
        public uint? AvgSpeedMetersPerHour { get; set; }
        public byte? AvgHeartRate { get; set; }
        public ushort? ElevationGain { get; set; }

        // Специфичные поля
        public float? AvgPower { get; set; }
        public int? AvgCadence { get; set; }
        public SkiStyle? StyleOfSkiing { get; set; }

        public SportTypes? SportType { get; set; }

        public List<RecordDataCollection> RecordDataCollections { get; set; } = new();
        public List<LapDataCollection> LapDataCollections { get; set; } = new();

        public string GetIdentifier()
        {
            if (!TimeStartUtc.HasValue)
                throw new NullReferenceException("Поле TimeStart не содержит данных");

            return $"{DeviceData.SerialNumber}_{TimeStartUtc.Value.AddSeconds(OffsetInSeconds)}";
        }
    }
}
