using System.ComponentModel.DataAnnotations;

namespace Server.API.Models.Group
{
    public class AssignTrainerDto
    {
        [Required]
        public string TrainerLogin { get; set; } = string.Empty;
    }
}
