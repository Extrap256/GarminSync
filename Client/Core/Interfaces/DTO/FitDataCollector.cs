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
        public DeviceData DeviceData { get; set; } = new();
        public DateTime? TimeStart { get; set; }
        public DateTime? TimeEnd { get; set; }
        public int? DistanceMeters { get; set; }
        public float? AvgSpeed { get; set; }
        public byte? AvgHeartRate { get; set; }
        public ushort? ElevationGain { get; set; }

        // Специфичные поля
        public float? AvgPower { get; set; }
        public int? AvgCadence { get; set; }
        public SkiStyle? StyleOfSkiing { get; set; }

        public Dynastream.Fit.Sport? SportType { get; set; }

        public List<RecordDataCollection> RecordDataCollections { get; set; } = new();
        public List<LapDataCollection> LapDataCollections { get; set; } = new();
    }
}
