using System.Globalization;
using Server.API.Models.Group;
using Server.Core.Interfaces;
using Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using EntityClassLibraryServer.Models;
using Server.Common.Exceptions;

namespace Server.Bot.Vk.Commands
{
    public class GroupCommand : IBotCommand
    {
        private readonly IVkMessageSender _messageSender;
        private readonly ApplicationDbContext _context;
        private readonly IGroupService _groupService;
        private readonly IReportService _reportService;
        private readonly ILogger<GroupCommand> _logger;

        private const int MaxMessageLength = 4000; // Лимит ВК — 4096, оставляем запас

        public GroupCommand(
            IVkMessageSender messageSender,
            ApplicationDbContext context,
            IGroupService groupService,
            IReportService reportService,
            ILogger<GroupCommand> logger)
        {
            _messageSender = messageSender;
            _context = context;
            _groupService = groupService;
            _reportService = reportService;
            _logger = logger;
        }

        public bool CanHandle(string text) =>
            !string.IsNullOrWhiteSpace(text) &&
            text.Trim().StartsWith("/group", StringComparison.OrdinalIgnoreCase);

        public async Task ExecuteAsync(long vkUserId, string text)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.VkId == vkUserId);
            if (user == null)
            {
                await _messageSender.SendMessageAsync(vkUserId, "Сначала войди: /login");
                return;
            }

            var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // /group или /group list
            if (parts.Length == 1 || (parts.Length == 2 && parts[1].Equals("list", StringComparison.OrdinalIgnoreCase)))
            {
                await HandleListAsync(vkUserId, user.UserId);
                return;
            }

            var subCommand = parts[1].ToLowerInvariant();

            try
            {
                switch (subCommand)
                {
                    case "create":
                        await HandleCreateAsync(vkUserId, user.UserId, parts);
                        break;
                    case "add":
                        await HandleAddAsync(vkUserId, user.UserId, parts);
                        break;
                    case "promote":
                        await HandlePromoteAsync(vkUserId, user.UserId, parts);
                        break;
                    case "report":
                        await HandleReportAsync(vkUserId, user.UserId, parts);
                        break;
                    default:
                        await SendHelpAsync(vkUserId);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка в /group {SubCommand} для {VkUserId}", subCommand, vkUserId);
                await _messageSender.SendMessageAsync(vkUserId, $"❌ Ошибка: {ex.Message}");
            }
        }

        // === СПРАВКА ===
        private async Task SendHelpAsync(long vkUserId)
        {
            await _messageSender.SendMessageAsync(vkUserId,
                "Работа с группами\n\n" +
                "Команды:\n" +
                "/group — мои группы\n" +
                "/group create <название> — создать\n" +
                "/group add <id> <логин> — добавить участника\n" +
                "/group promote <id> <логин> — назначить тренером\n" +
                "/group report <id> — отчёт за месяц\n" +
                "/group report <id> 7 — за 7 дней\n" +
                "/group report <id> 2026-01-01 2026-01-31 — за период");
        }

        // === /group (list) ===
        private async Task HandleListAsync(long vkUserId, int userId)
        {
            var user = await _context.Users
                .Include(u => u.TrainedGroups)
                .Include(u => u.Groups)
                .FirstOrDefaultAsync((User u) => u.UserId == userId);
            if (user == null)
                throw new NotFoundException("Пользователь не найден");

            var groupsT = user.TrainedGroups;
            var groupsM = user.Groups;

            if (!(groupsT.Count != 0 || groupsM.Count != 0))
            {
                await _messageSender.SendMessageAsync(vkUserId,
                    "Ты не состоишь ни в одной группе.\n\n" +
                    "Создай свою: /group create <название>");
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Мои группыn");

            foreach (var g in groupsT)
            {
                sb.AppendLine($"#{g.GroupId} — {g.Name}");
                sb.AppendLine($"   Роль: Тренер\n");
            }
            foreach (var g in groupsM)
            {
                sb.AppendLine($"#{g.GroupId} — {g.Name}");
                sb.AppendLine($"   Роль: Спортсмен\n");
            }

            await SendLongMessageAsync(vkUserId, sb.ToString());
        }

        // === /group create <name> ===
        private async Task HandleCreateAsync(long vkUserId, int userId, string[] parts)
        {
            if (parts.Length < 3)
            {
                await _messageSender.SendMessageAsync(vkUserId,
                    "Формат: /group create <название>\n\n" +
                    "Пример: /group create Марафонцы 2026");
                return;
            }

            // Название = всё что после "/group create "
            var name = string.Join(' ', parts.Skip(2)).Trim();
            if (name.Length > 100)
            {
                await _messageSender.SendMessageAsync(vkUserId,
                    "Название слишком длинное (макс. 100 символов)");
                return;
            }

            var dto = new CreateGroupDto
            {
                Name = name,
                IsPublic = false // По умолчанию частная
            };

            var group = await _groupService.CreateGroupAsync(userId, dto);

            _logger.LogInformation("Пользователь {UserId} создал группу {GroupId} ({Name})",
                userId, group.GroupId, name);

            await _messageSender.SendMessageAsync(vkUserId,
                $"Группа создана!\n\n" +
                $"ID: {group.GroupId}\n" +
                $"Название: {name}\n" +
                $"Ты — тренер и участник\n\n" +
                $"Добавить спортсмена:\n" +
                "`/group add {group.GroupId} <логин>`");
        }

        // === /group add <id> <login> ===
        private async Task HandleAddAsync(long vkUserId, int userId, string[] parts)
        {
            if (parts.Length < 4)
            {
                await _messageSender.SendMessageAsync(vkUserId,
                    "Формат: `/group add <id_группы> <логин>`\n\n" +
                    "Пример: `/group add 15 ivan_petrov`");
                return;
            }

            if (!int.TryParse(parts[2], out int groupId))
            {
                await _messageSender.SendMessageAsync(vkUserId, "ID группы должен быть числом");
                return;
            }

            var memberLogin = parts[3];

            // Проверка прав: только тренер может добавлять
            var role = await _groupService.GetUserRoleInGroupAsync(groupId, userId);
            if (role != "trainer")
            {
                await _messageSender.SendMessageAsync(vkUserId,
                    "Только тренер группы может добавлять участников");
                return;
            }

            await _groupService.AddMemberToGroupAsync(groupId, memberLogin);

            _logger.LogInformation("Тренер {UserId} добавил {Login} в группу {GroupId}",
                userId, memberLogin, groupId);

            await _messageSender.SendMessageAsync(vkUserId,
                $"Пользователь `{memberLogin}` добавлен в группу #{groupId}");
        }

        // === /group promote <id> <login> ===
        private async Task HandlePromoteAsync(long vkUserId, int userId, string[] parts)
        {
            if (parts.Length < 4)
            {
                await _messageSender.SendMessageAsync(vkUserId,
                    "Формат: `/group promote <id_группы> <логин>`\n\n" +
                    "Пример: `/group promote 15 ivan_petrov`");
                return;
            }

            if (!int.TryParse(parts[2], out int groupId))
            {
                await _messageSender.SendMessageAsync(vkUserId, "ID группы должен быть числом");
                return;
            }

            var trainerLogin = parts[3];

            var role = await _groupService.GetUserRoleInGroupAsync(groupId, userId);
            if (role != "trainer")
            {
                await _messageSender.SendMessageAsync(vkUserId,
                    "Только тренер группы может назначать других тренеров");
                return;
            }

            await _groupService.AddTrainerToGroupAsync(groupId, trainerLogin);

            _logger.LogInformation("Тренер {UserId} назначил {Login} тренером группы {GroupId}",
                userId, trainerLogin, groupId);

            await _messageSender.SendMessageAsync(vkUserId,
                $"Пользователь `{trainerLogin}` теперь тренер группы #{groupId}");
        }

        // === /group report <id> [days | from to] ===
        private async Task HandleReportAsync(long vkUserId, int userId, string[] parts)
        {
            if (parts.Length < 3)
            {
                await _messageSender.SendMessageAsync(vkUserId,
                    "Формат:\n" +
                    "`/group report <id>` — за месяц\n" +
                    "`/group report <id> 7` — за 7 дней\n" +
                    "`/group report <id> 2026-01-01 2026-01-31`");
                return;
            }

            if (!int.TryParse(parts[2], out int groupId))
            {
                await _messageSender.SendMessageAsync(vkUserId, "ID группы должен быть числом");
                return;
            }

            // Проверка: пользователь должен быть в группе
            if (!await _groupService.IsUserInGroupAsync(groupId, userId))
            {
                await _messageSender.SendMessageAsync(vkUserId,
                    "Ты не состоишь в этой группе");
                return;
            }

            // Парсим даты (переиспользуем логику из ReportCommand)
            var (from, to, error) = ParseDateRange(parts, startIndex: 3);
            if (error != null)
            {
                await _messageSender.SendMessageAsync(vkUserId, error);
                return;
            }

            var report = await _reportService.GetGroupReportAsync(groupId, from, to);
            await SendLongMessageAsync(vkUserId, report);
        }

        // === Переиспользуемая логика парсинга дат ===
        private static (DateTime from, DateTime to, string? error) ParseDateRange(string[] parts, int startIndex)
        {
            var to = DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);

            // Нет параметров после groupId
            if (parts.Length <= startIndex)
            {
                return (to.AddMonths(-1), to, null);
            }

            // Одно число — количество дней
            if (parts.Length == startIndex + 1 && int.TryParse(parts[startIndex], out int days))
            {
                if (days < 1 || days > 3650)
                    return (default, default, "Количество дней: от 1 до 3650");
                return (to.AddDays(-days), to, null);
            }

            // Две даты
            if (parts.Length >= startIndex + 2)
            {
                string[] formats = { "yyyy-MM-dd", "dd.MM.yyyy" };

                if (DateTime.TryParseExact(parts[startIndex], formats, CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var from) &&
                    DateTime.TryParseExact(parts[startIndex + 1], formats, CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsedTo))
                {
                    var toEndOfDay = parsedTo.Date.AddDays(1).AddTicks(-1);
                    if (from >= toEndOfDay)
                        return (default, default, "Дата начала должна быть раньше даты окончания");
                    return (from, toEndOfDay, null);
                }

                return (default, default, "Формат дат: `2026-01-01` или `01.01.2026`");
            }

            return (default, default, "Неверный формат");
        }

        // === Отправка длинных сообщений (разбиваем при необходимости) ===
        private async Task SendLongMessageAsync(long vkUserId, string message)
        {
            if (message.Length <= MaxMessageLength)
            {
                await _messageSender.SendMessageAsync(vkUserId, message);
                return;
            }

            // Разбиваем по строкам
            var lines = message.Split('\n');
            var chunk = new System.Text.StringBuilder();

            foreach (var line in lines)
            {
                if (chunk.Length + line.Length + 1 > MaxMessageLength)
                {
                    await _messageSender.SendMessageAsync(vkUserId, chunk.ToString());
                    chunk.Clear();
                }
                chunk.AppendLine(line);
            }

            if (chunk.Length > 0)
                await _messageSender.SendMessageAsync(vkUserId, chunk.ToString());
        }
    }
}
