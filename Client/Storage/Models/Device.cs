using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Sqlite;

namespace Client.Storage.Models
{
    [Index("SerialNumber", IsUnique = true)]
    public class Device
    {
        [Key]
        public int DeviceId {  get; set; }
        public int UserId {  get; set; }
        public string Name {  get; set; }
        public string Model {  get; set; }
        public string SerialNumber {  get; set; }
        public bool IsVirtual {  get; set; }
        public string Description { get; set; }
        public List<Workout> Workouts { get; set; }
    }
}
