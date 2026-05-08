namespace Client.Storage.Models
{
    public class WorkoutCycling : Workout
    {
        public float? AvgPower {  get; set; }
        public int? AvgCadence { get; set; }
    }
}
