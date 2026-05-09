using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Client.Storage.Models
{
    [Index("WorkoutId")]
    public abstract class Lap
    {
        [Key]
        public int LapId {  get; set; }
        public int WorkoutId {  get; set; }
        public Workout? Workout { get; set; }
        public DateTime TimeEndStart {  get; set; }
        public uint? DistanceMeters {  get; set; }
        public uint? AvrSpeedMetersPerHour {  get; set; }
        public byte? AvrHeartRate {  get; set; }
        public int? ElevationGain {  get; set; }
    }
}
