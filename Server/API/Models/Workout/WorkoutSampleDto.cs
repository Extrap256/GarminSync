namespace Server.API.Models.Workout
{
    public class WorkoutSampleDto
    {
        public uint? TimeSeconds { get; set; }

        public uint? DistanceMeters { get; set; }

        public uint? SpeedMetersPerHour { get; set; }

        public byte? HeartRate { get; set; }

        public ushort? Altitude { get; set; }

        public int? Latitude { get; set; }

        public int? Longitude { get; set; }

        public string Discriminator { get; set; } = null!;

        public byte? Cadence { get; set; }

        public int? Power { get; set; }

        public byte? WorkoutSamplesRunningCadence { get; set; }
    }
}
