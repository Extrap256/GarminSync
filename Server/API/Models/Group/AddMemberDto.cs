using System.ComponentModel.DataAnnotations;

namespace Server.API.Models.Group
{
    public class AddMemberDto
    {
        [Required]
        public string MemberLogin { get; set; } = string.Empty;
    }
}
