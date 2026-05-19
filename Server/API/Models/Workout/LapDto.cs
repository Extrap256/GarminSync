namespace Server.API.Models.Workout
{
    public class LapDto
    {
        public DateTime TimeEnd { get; set; }

        public uint? DistanceMeters { get; set; }

        public uint? AvrSpeedMetersPerHour { get; set; }

        public byte? AvgHeartRate { get; set; }

        public int? ElevationGain { get; set; }

        public string Discriminator { get; set; } = null!;

        public byte? AvgCadence { get; set; }

        public int? AvgPower { get; set; }

        public byte? LapRunningAvgCadence { get; set; }
    }
}
