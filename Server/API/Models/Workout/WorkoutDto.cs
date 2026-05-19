using System.ComponentModel.DataAnnotations;

namespace Server.API.Models.Workout
{
    public class WorkoutDto
    {
        [Required]
        public DateTime TimeStart { get; set; }

        [Required]
        public DateTime TimeEnd { get; set; }

        public string Identifier { get; set; } = null!;

        public uint? SerialNumberDevice { get; set; }
        public uint? DistanceMeters { get; set; }
        public double? Duration { get; set; }
        public byte? AvgHeartRate { get; set; }
        public uint? AvgSpeedMetersPerHour { get; set; }
        public int? ElevationGain { get; set; }
        public string? Description { get; set; }
        [Required]
        public string SportType { get; set; } = string.Empty;
        public float? AvgPower { get; set; }
        public int? AvgCadence { get; set; }
        public int? WorkoutRunningAvgCadence { get; set; }
        public int? StyleOfSkiing { get; set; }

        //public double? Calories { get; set; }

        public List<WorkoutSampleDto>? WorkoutSamples { get; set; }
        public List<LapDto>? Laps { get; set; }
    }
}
