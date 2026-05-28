namespace Server.Bot.Vk.Commands
{
    public class StartCommand : IBotCommand
    {
        private readonly IVkMessageSender _messageSender;

        public StartCommand(IVkMessageSender messageSender)
            => _messageSender = messageSender;

        public bool CanHandle(string text) =>
            !string.IsNullOrWhiteSpace(text) &&
            text.Trim().StartsWith("/start", StringComparison.OrdinalIgnoreCase);

        public async Task ExecuteAsync(long vkUserId, string text)
        {
            await _messageSender.SendMessageAsync(vkUserId, GetHelpMessage());
        }

        public static string GetHelpMessage()
        {
            return "GarminSync Bot — твой помощник в тренировках\n\n" +
            "Аккаунт:\n" +
            "  /register <логин> <пароль> — регистрация\n" +
            "  /login <логин> <пароль> — вход\n" +
            "  /logout — выход\n" +
            "  /profile — мой профиль (просмотр и редактирование)\n\n" +
            "Тренировки:\n" +
            "  /workout — добавить тренировку вручную\n" +
            "  /report — личный отчёт за месяц\n" +
            "  /report 7 — за последние N дней\n" +
            "  /report 2026-01-01 2026-01-31 — за период\n\n" +
            "Группы:\n" +
            "  /group — мои группы\n" +
            "  /group create <название> — создать группу\n" +
            "  /group add <id> <логин> — добавить участника\n" +
            "  /group promote <id> <логин> — назначить тренером\n" +
            "  /group report <id> — отчёт по группе\n\n" +
            "Прочее:\n" +
            "  /cancel — отменить текущее действие\n" +
            "  /start — показать это сообщение\n\n" +
            "Синхронизация с часами Garmin — через Windows-клиент";
        }
            
    }
}
