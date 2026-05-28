using VkNet.Abstractions;
using VkNet.Model;

namespace Server.Bot.Vk
{
    public class VkMessageSender : IVkMessageSender
    {
        private readonly IVkApi _vkApi;

        public VkMessageSender(IVkApi vkApi)
        {
            _vkApi = vkApi;
        }

        public async Task SendMessageAsync(long vkUserId, string message)
        {
            await _vkApi.Messages.SendAsync(new MessagesSendParams
            {
                PeerId = vkUserId,
                Message = message,
                RandomId = Environment.TickCount
            });
        }
    }
}
