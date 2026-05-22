using System.ComponentModel.DataAnnotations;

namespace Server.API.Models.User
{
    public class UserUpdateDto
    {
        [MaxLength(100)]
        public string? FirstName { get; set; }
        [MaxLength(100)]
        public string? LastName { get; set; }
        public int? YearOfBirth { get; set; }
    }
}
