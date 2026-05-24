using System.Text.Json.Serialization;

namespace Client.Core.Interfaces.DTO.API
{
    public class SyncWorkoutRequest
    {
        public DateTime TimeStart {  get; set; }
        public DateTime TimeEnd { get; set; }
        public string Identifier { get; set; } = null!;
        public string SportType { get; set; } = null!;
        public uint SerialNumberDevice { get; set; }
        public uint? DistanceMeters { get; set; }
        public int Duration { get; set; } // Секунды
        public byte? AvgHeartRate { get; set; }
        public uint? AvgSpeedMetersPerHour { get; set; }
        public int? ElevationGain { get; set; }
        public string? Description { get; set; }
        public int? AvgCadence { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<SyncWorkoutSampleRequest>? WorkoutSamples { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<SyncLapRequest>? Laps { get; set; }
    }
}
