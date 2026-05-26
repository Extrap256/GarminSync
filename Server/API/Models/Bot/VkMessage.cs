using System.Text.Json.Serialization;

namespace Server.API.Models.Bot
{
    public class VkMessage
    {
        [JsonPropertyName("from_id")]
        public long FromId {  get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("peer_id")]
        public long PeerId { get; set; }

        [JsonPropertyName("conversation_message_id")]
        public long ConversationMessageId { get; set; }
    }
}
