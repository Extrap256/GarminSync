namespace Client.Storage.Models
{
    public class WorkoutCycling : Workout
    {
        public float? AvrPower {  get; set; }
        public int? AvrCadence { get; set; }
    }
}
