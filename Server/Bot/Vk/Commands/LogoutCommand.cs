using Microsoft.EntityFrameworkCore;
using Server.Infrastructure.Data;

namespace Server.Bot.Vk.Commands
{
    public class LogoutCommand : IBotCommand
    {
        private readonly IVkMessageSender _messageSender;
        private readonly ApplicationDbContext _context;

        public LogoutCommand(IVkMessageSender messageSender, ApplicationDbContext context)
        {
            _messageSender = messageSender;
            _context = context;
        }

        public bool CanHandle(string text) =>
            !string.IsNullOrWhiteSpace(text) &&
            text.Trim().Equals("/logout", StringComparison.OrdinalIgnoreCase);

        public async Task ExecuteAsync(long vkUserId, string text)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.VkId == vkUserId);
            if (user == null)
            {
                await _messageSender.SendMessageAsync(vkUserId, "Ты не авторизован");
                return;
            }

            user.VkId = null;
            await _context.SaveChangesAsync();

            await _messageSender.SendMessageAsync(vkUserId,
                "Выход выполнен. До встречи!");
        }
    }
}
