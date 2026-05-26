using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace EntityClassLibraryServer.Models
{
    [Index("BindingId")]
    public class VkUserBinding
    {
        [Key]
        public int BindingId { get; set; }

        /// <summary>
        /// Id пользователя VK
        /// </summary>
        public int VkUserId {  get; set; }

        /// <summary>
        /// Id пользователя в системе
        /// </summary>
        public int UserId {  get; set; }
        public User? User { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt {  get; set; }
    }
}
