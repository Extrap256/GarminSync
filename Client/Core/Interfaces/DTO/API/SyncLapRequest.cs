using System;
using System.Collections.Generic;
using System.Text;

namespace Client.Core.Interfaces.DTO.API
{
    public class SyncLapRequest
    {
        public DateTime TimeEnd { get; set; }
        public uint? DistanceMeters { get; set; }
        public uint? AvrSpeedMetersPerHour { get; set; }
        public byte? AvgHeartRate { get; set; }
        public int? ElevationGain { get; set; }
        public string Discriminator { get; set; } = null!;
    }
}
