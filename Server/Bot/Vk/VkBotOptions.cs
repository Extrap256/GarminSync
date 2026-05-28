namespace Server.Bot.Vk
{
    public class VkBotOptions
    {
        public const string SectionName = "VkBot";

        /// <summary>
        /// Токен доступа сообщества
        /// </summary>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// Секретный ключ для проверки Callback (задаётся в настройках ВК)
        /// </summary>
        public string SecretKey { get; set; } = string.Empty;

        /// <summary>
        /// Строка подтверждения сервера (из настроек Callback API ВК)
        /// </summary>
        public string ConfirmationCode { get; set; } = "4f8e4d2d";

        /// <summary>
        /// ID группы
        /// </summary>
        public long GroupId { get; set; }
    }
}
