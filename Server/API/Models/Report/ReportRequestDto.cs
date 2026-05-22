using System.ComponentModel.DataAnnotations;

namespace Server.API.Models.Report
{
    public class ReportRequestDto
    {
        [Required]
        public DateTime From { get; set; }
        [Required]
        public DateTime To {  get; set; }
    }
}
