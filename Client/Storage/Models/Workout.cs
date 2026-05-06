using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Client.Storage.Models
{
    [Index("Identifer", IsUnique = true)]
    [Index("TimeStart", IsDescending = [true])]
    public abstract class Workout
    {
        [Key]
        public int WorkoutId {  get; set; }
        public int DeviceId {  get; set; }
        public string Identifer {  get; set; }  //(DeviceId + Timestart)
        public DateTime TimeStart {  get; set; }
        public DateTime TimeEnd { get; set; }
        public float? Distance {  get; set; }
        public int? AvrSpeed {  get; set; }
        public byte? AvrHeartRate {  get; set; }
        public int? ElevationGain {  get; set; }
        public string? Description {  get; set; }
        
        public List<WorkoutSamples> WorkoutSamples { get; set; }
        List<Lap> Laps { get; set; }
    }
}
