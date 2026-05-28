using Server.Common.Helpers;
using Server.Infrastructure.Data;
using EntityClassLibraryServer.Models;
using Microsoft.EntityFrameworkCore;

using VkNet;

namespace Server.Bot.Vk.Commands
{
    public class LoginCommand : IBotCommand
    {
        private readonly IVkMessageSender _messageSender;
        private readonly ApplicationDbContext _context;

        public LoginCommand(IVkMessageSender messageSender, ApplicationDbContext context)
        {
            _messageSender = messageSender;
            _context = context;
        }

        public bool CanHandle(string text) =>
            !string.IsNullOrWhiteSpace(text) &&
            text.Trim().StartsWith("/login", StringComparison.OrdinalIgnoreCase);

        public async Task ExecuteAsync(long vkUserId, string text)
        {
            var existing = await _context.Users.FirstOrDefaultAsync(u => u.VkId == vkUserId);
            if (existing != null)
            {
                await _messageSender.SendMessageAsync(vkUserId,
                    $"Ты уже вошёл как {existing.Login}");
                return;
            }

            var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
            {
                await _messageSender.SendMessageAsync(vkUserId,
                    "Формат:\n/login <логин> <пароль>");
                return;
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Login == parts[1]);
            if (user == null || !PasswordHelper.VerifyPassword(parts[2], user.Hash))
            {
                await _messageSender.SendMessageAsync(vkUserId,
                    "Неверный логин или пароль");
                return;
            }

            if (user.VkId.HasValue && user.VkId != vkUserId)
            {
                await _messageSender.SendMessageAsync(vkUserId,
                    "⚠Этот аккаунт уже привязан к другому VK");
                return;
            }

            user.VkId = vkUserId;
            await _context.SaveChangesAsync();

            await _messageSender.SendMessageAsync(vkUserId,
                $"Вход выполнен как {user.Login}");
        }
    }
}
