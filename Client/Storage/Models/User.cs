using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Client.Storage.Models
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
        [Required]
        public ushort YearOfBirth {  get; set; }
        public List<Device> Devices { get; set; } = [];
    }
}
