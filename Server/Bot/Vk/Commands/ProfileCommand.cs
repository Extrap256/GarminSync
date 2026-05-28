using Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Server.Bot.Vk.Commands
{
    public class ProfileCommand : IBotCommand
    {
        private readonly IVkMessageSender _messageSender;
        private readonly ApplicationDbContext _context;

        public ProfileCommand(IVkMessageSender messageSender, ApplicationDbContext context)
        {
            _messageSender = messageSender;
            _context = context;
        }

        public bool CanHandle(string text) =>
            !string.IsNullOrWhiteSpace(text) &&
            text.Trim().StartsWith("/profile", StringComparison.OrdinalIgnoreCase);

        public async Task ExecuteAsync(long vkUserId, string text)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.VkId == vkUserId);
            if (user == null)
            {
                await _messageSender.SendMessageAsync(vkUserId, "⚠️ Сначала войди: /login");
                return;
            }

            var parts = text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

            // === /profile (без параметров) — показать профиль ===
            if (parts.Length == 1)
            {
                var msg = $"**Твой профиль**\n\n" +
                          $"Логин: {user.Login}\n" +
                          $"Имя: {user.FirstName ?? "—"}\n" +
                          $"Фамилия: {user.LastName ?? "—"}\n" +
                          $"Год рождения: {user.YearOfBirth?.ToString() ?? "—"}\n" +
                          $"Регистрация: {user.CreatedAt.ToLocalTime():dd.MM.yyyy}\n\n" +
                          $"**Редактирование:**\n" +
                          "`/profile name Иван` — имя\n" +
                          "`/profile lastname Иванов` — фамилия\n" +
                          "`/profile year 1995` — год рождения";
                await _messageSender.SendMessageAsync(vkUserId, msg);
                return;
            }

            // === /profile <field> <value> ===
            var remainder = parts[1].Trim();
            var spaceIdx = remainder.IndexOf(' ');

            if (spaceIdx == -1)
            {
                await _messageSender.SendMessageAsync(vkUserId,
                    "Формат: `/profile <поле> <значение>`\n\n" +
                    "Поля: name, lastname, year");
                return;
            }

            var field = remainder[..spaceIdx].ToLowerInvariant();
            var value = remainder[(spaceIdx + 1)..].Trim();

            switch (field)
            {
                case "name":
                    if (value.Length > 100)
                    {
                        await _messageSender.SendMessageAsync(vkUserId, "❌ Имя слишком длинное");
                        return;
                    }
                    user.FirstName = value;
                    break;

                case "lastname":
                    if (value.Length > 100)
                    {
                        await _messageSender.SendMessageAsync(vkUserId, "Фамилия слишком длинная");
                        return;
                    }
                    user.LastName = value;
                    break;

                case "year":
                    if (!int.TryParse(value, out int year) || year < 1920 || year > DateTime.UtcNow.Year)
                    {
                        await _messageSender.SendMessageAsync(vkUserId,
                            "Неверный год. Допустимый диапазон: 1920–" + DateTime.UtcNow.Year);
                        return;
                    }
                    user.YearOfBirth = (ushort)year;
                    break;

                default:
                    await _messageSender.SendMessageAsync(vkUserId,
                        "Неизвестное поле. Доступные: name, lastname, year");
                    return;
            }

            await _context.SaveChangesAsync();
            await _messageSender.SendMessageAsync(vkUserId, $"Обновлено: {field} = {value}");
        }
    }
}
