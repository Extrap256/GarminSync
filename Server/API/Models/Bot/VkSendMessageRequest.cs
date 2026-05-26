using System.Text.Json.Serialization;

namespace Server.API.Models.Bot
{
    /// <summary>
    /// Запрос для отправки сообщений VK
    /// </summary>
    public class VkSendMessageRequest
    {
        [JsonPropertyName("peer_id")]
        public long PeerId { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("random_id")]
        public int RandomId { get; set; }
    }
}
