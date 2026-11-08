using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace EntityClassLibraryClient.Models
{
    [Index("SerialNumber", IsUnique = true)]
    public class Device
    {
        [Key]
        public int DeviceId {  get; set; }
        public string? Name {  get; set; }
        public ushort? Model {  get; set; }
        public uint? SerialNumber {  get; set; }
        public bool IsVirtual {  get; set; }
        public string? Description { get; set; }
        public List<Workout> Workouts { get; set; } = new();
    }
}
