namespace Server.Bot.Vk.Session
{
    public class BotSession
    {
        public string CommandName { get; set; } = string.Empty;
        public int Step { get; set; }
        public Dictionary<string, string> Data { get; set; } = new();
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;

        public bool IsExpired => (DateTime.UtcNow - LastActivity).TotalMinutes > 30;
    }
}
