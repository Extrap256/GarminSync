namespace Client.Storage.Models
{
    public class LapCycling : Lap
    {
        public byte? Cadence { get; set; }
        public int? AvrPower { get; set; }
    }
}
