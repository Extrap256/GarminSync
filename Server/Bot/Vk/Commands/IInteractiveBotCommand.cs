using Server.Bot.Vk.Session;

namespace Server.Bot.Vk.Commands
{
    public interface IInteractiveBotCommand : IBotCommand
    {
        /// <summary>
        /// Обрабатывает очередной шаг диалога
        /// </summary>
        Task HandleStepAsync(long vkUserId, BotSession session, string input);
    }
}
