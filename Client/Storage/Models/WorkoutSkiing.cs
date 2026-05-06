namespace Client.Storage.Models
{
    public class WorkoutSkiing : Workout
    {
        public enum Styles
        {
            Skating,
            Classic,
            DoublePoling
        }
        
        public Styles StyleOfSkiing {  get; set; }
    }
}
