namespace EntityClassLibraryServer.Models
{
    public class WorkoutSamplesCycling : WorkoutSamples
    {
        public byte? Cadence { get; set; }
        public int? Power { get; set; }
    }
}
