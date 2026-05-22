using System.ComponentModel.DataAnnotations;

namespace Server.API.Models.Group
{
    public class CreateGroupDto
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        [MaxLength(500)]
        public string? Description {  get; set; }
        /// <summary>
        /// true - по умолчанию все тренера
        /// false - все участники
        /// </summary>
        public bool IsPublic { get; set; } = false;
    }
}
