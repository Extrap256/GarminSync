namespace Server.Bot.Vk
{
    public interface IVkMessageSender
    {
        Task SendMessageAsync(long vkUserId, string message);
    }
}
