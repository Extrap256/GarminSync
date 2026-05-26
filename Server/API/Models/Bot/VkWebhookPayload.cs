using System.Text.Json.Serialization;

namespace Server.API.Models.Bot
{
    public class VkWebhookPayload
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("object")]
        public VkEventObject? EventOdject {  get; set; }

        [JsonPropertyName("group_id")]
        public long GroupId {  get; set; }
    }
}
