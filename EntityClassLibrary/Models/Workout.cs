using System.ComponentModel.DataAnnotations;
using EntityClassLibraryServer.Models;
using Microsoft.EntityFrameworkCore;

namespace EntityClassLibraryServer.Models
{
    [Index("Identifier")]
    [Index("TimeStart", IsDescending = [true])]
    public abstract class Workout
    {
        private string _identifier = null!;
        [Key]
        public int WorkoutId {  get; set; }
        public int DeviceId {  get; set; }
        public Device? Device { get; set; }
        public uint SerialNumberDevice {  get; set; }
        public string Identifier { get => _identifier; }  // DeviceId_TimeStart
        public DateTime TimeStart {  get; set; }
        public DateTime TimeEnd { get; set; }
        public uint? DistanceMeters {  get; set; }
        public uint? AvgSpeedMetersPerHour {  get; set; }
        public byte? AvgHeartRate {  get; set; }
        public int? ElevationGain {  get; set; }
        public string? Description {  get; set; }

        public List<WorkoutSamples>? WorkoutSamples { get; set; }
        public List<Lap>? Laps { get; set; }

        public void SetIdentifier()
        {
            if (TimeStart == default)
                throw new InvalidOperationException("TimeStart must be set");

            _identifier = $"{SerialNumberDevice}_{TimeStart}";
        }
    }
}
