using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Client.Storage.Models
{
    [Index("WorkoutId", "TimeSeconds")]
    public abstract class WorkoutSamples
    {
        [Key]
        public int WorkoutSamplesId { get; set; }
        public int WorkoutId {  get; set; }
        public uint? TimeSeconds {  get; set; }
        public uint? DistanceMeters {  get; set; }
        public uint? SpeedMetersPerHour { get; set; }
        public byte? HeartRate {  get; set; }
        public ushort? Altitude {  get; set; }
        public int? Latitude {  get; set; }
        public int? Longitude { get; set; }
    }
}
