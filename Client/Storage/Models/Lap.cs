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
        public uint TimeStartSeconds {  get; set; }
        public int? DistanceMeters {  get; set; }
        public int? AvrSpeedMetersPerHour {  get; set; }
        public byte? AvrHeartRate {  get; set; }
        public int? ElevationGain {  get; set; }
    }
}
