using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace EntityClassLibraryServer.Models
{
    [Index("WorkoutId")]
    public abstract class Lap
    {
        [Key]
        public int LapId {  get; set; }
        public int WorkoutId {  get; set; }
        public Workout? Workout { get; set; }
        public DateTime TimeEnd {  get; set; }
        public uint? DistanceMeters {  get; set; }
        public uint? AvgSpeedMetersPerHour {  get; set; }
        public byte? AvgHeartRate {  get; set; }
        public int? ElevationGain {  get; set; }
    }
}
