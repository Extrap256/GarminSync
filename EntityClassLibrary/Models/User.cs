using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace EntityClassLibrary.Models
{
    [Index("Login", IsUnique = true)]
    [Index("UserId")]
    public class User
    {
        [Key]
        public int UserId {  get; set; }
        [Required]
        public string Login { get; set; } = null!;
        [Required]
        public string Hash { get; set; } = null!;
        [Required]
        public string FirstName {  get; set; } = null!;
        [Required]
        public string LastName {  get; set; } = null!;
        public ushort? YearOfBirth {  get; set; }
        public List<Device> Devices { get; set; } = new();
        public List<Group> Groups { get; set; } = new();

        public List<Group> TrainedGroups {  get; set; } = new();
        public List<Group> CreatedGroups { get; set; } = new();
    }
}
