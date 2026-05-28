namespace Server.Bot.Vk.Commands
{
    public interface IBotCommand
    {
        bool CanHandle(string text);
        Task ExecuteAsync(long vkUserId, string text);
    }
}
