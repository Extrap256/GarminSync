using System.Text.Json.Serialization;

namespace Server.API.Models.Bot
{
    public class VkEventObject
    {
        [JsonPropertyName("message")]
        public VkMessage? Message {  get; set; }
    }
}
