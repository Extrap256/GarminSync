using EntityClassLibraryServer.Models;
using Microsoft.EntityFrameworkCore;
using Server.Common.Helpers;
using Server.Infrastructure.Data;
using VkNet.Abstractions;
using VkNet.Enums.Filters;

namespace Server.Bot.Vk.Commands
{
    public class RegisterCommand : IBotCommand
    {
        private readonly IVkMessageSender _messageSender;
        private readonly ApplicationDbContext _context;
        private readonly IVkApi _vkApi;
        private readonly ILogger<RegisterCommand> _logger;

        public RegisterCommand(
            IVkMessageSender messageSender,
            ApplicationDbContext context,
            IVkApi vkApi,  // ← Добавили
            ILogger<RegisterCommand> logger)
        {
            _messageSender = messageSender;
            _context = context;
            _vkApi = vkApi;
            _logger = logger;
        }

        public bool CanHandle(string text) =>
            !string.IsNullOrWhiteSpace(text) &&
            text.Trim().StartsWith("/register", StringComparison.OrdinalIgnoreCase);

        public async Task ExecuteAsync(long vkUserId, string text)
        {
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.VkId == vkUserId);

            if (existingUser != null)
            {
                await _messageSender.SendMessageAsync(vkUserId,
                    $"Ты уже зарегистрирован как {existingUser.Login}.\nИспользуй /logout чтобы выйти.");
                return;
            }

            var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
            {
                await _messageSender.SendMessageAsync(vkUserId,
                    "Формат команды:\n/register <логин> <пароль>\n\n" +
                    "Пример: /register ivan mypass123\n\n" +
                    "Пароль минимум 6 символов");
                return;
            }

            var login = parts[1];
            var password = parts[2];

            if (password.Length < 6)
            {
                await _messageSender.SendMessageAsync(vkUserId,
                    "Пароль должен быть не менее 6 символов");
                return;
            }

            if (await _context.Users.AnyAsync(u => u.Login == login))
            {
                await _messageSender.SendMessageAsync(vkUserId,
                    "Пользователь с таким логином уже существует");
                return;
            }

            // 🎯 Получаем имя и фамилию из ВК
            string? firstName = null;
            string? lastName = null;
            int? yearOfBirth = null;

            try
            {
                var vkUsers = await _vkApi.Users.GetAsync(
                    new[] { vkUserId.ToString() },
                    ProfileFields.FirstName | ProfileFields.LastName | ProfileFields.BirthDate);

                var vkUser = vkUsers.FirstOrDefault();
                if (vkUser != null)
                {
                    firstName = vkUser.FirstName;
                    lastName = vkUser.LastName;

                    // Пробуем распарсить год рождения
                    if (vkUser.BirthDate != null)
                    {
                        var bdParts = vkUser.BirthDate.Split('.');
                        if (bdParts.Length == 3 && int.TryParse(bdParts[2], out int yob))
                            yearOfBirth = yob;
                        else if (bdParts.Length == 2)
                        {
                            // Только день.месяц — год не указан
                        }
                    }

                    _logger.LogInformation(
                        "Получены данные из ВК: {FirstName} {LastName} ({Year})",
                        firstName, lastName, yearOfBirth);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось получить профиль ВК для {VkId}", vkUserId);
                // Не критично — просто продолжим без имени
            }

            var user = new User
            {
                Login = login,
                Hash = PasswordHelper.HashPassword(password),
                VkId = vkUserId,
                FirstName = firstName,   // может быть null — это ОК
                LastName = lastName,
                YearOfBirth = (ushort?)yearOfBirth,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("VK user {VkId} зарегистрирован как {Login}", vkUserId, login);

            var nameInfo = !string.IsNullOrEmpty(firstName)
                ? $"\nИмя: {firstName} {lastName}".TrimEnd()
                : "\n\nУкажи имя через: /profile";

            await _messageSender.SendMessageAsync(vkUserId,
                $"Регистрация успешна!\n" +
                $"Логин: {login}" +
                $"{nameInfo}\n\n" +
                $"Теперь доступны все команды бота.");
        }
    }
}
