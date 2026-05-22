using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace EntityClassLibrary.Models
{
    [Index("GroupId")]
    public class Group
    {
        [Key]
        public int GroupId {  get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<User> Members { get; set; } = new();
        public List<User> Trainers { get; set; } = new();
        public DateTime CreatedAt {  get; set; }
        public int? CreatorId { get; set; }
        public User? Creator {  get; set; }
    }
}
