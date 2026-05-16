namespace EntityClassLibrary.Models
{
    public class LapCycling : Lap
    {
        public byte? AvgCadence { get; set; }
        public int? AvgPower { get; set; }
    }
}
