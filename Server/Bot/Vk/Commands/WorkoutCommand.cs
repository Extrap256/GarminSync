using EntityClassLibraryServer.Models;
using Server.Bot.Vk.Session;
using Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Server.Bot.Vk.Commands
{
    public class WorkoutCommand : IInteractiveBotCommand
    {
        private readonly IVkMessageSender _messageSender;
        private readonly ApplicationDbContext _context;
        private readonly BotSessionStore _sessionStore;
        private readonly ILogger<WorkoutCommand> _logger;

        private const string CommandName = nameof(WorkoutCommand);

        public WorkoutCommand(
            IVkMessageSender messageSender,
            ApplicationDbContext context,
            BotSessionStore sessionStore,
            ILogger<WorkoutCommand> logger)
        {
            _messageSender = messageSender;
            _context = context;
            _sessionStore = sessionStore;
            _logger = logger;
        }

        public bool CanHandle(string text) =>
            !string.IsNullOrWhiteSpace(text) &&
            text.Trim().Equals("/workout", StringComparison.OrdinalIgnoreCase);

        public async Task ExecuteAsync(long vkUserId, string text)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.VkId == vkUserId);
            if (user == null)
            {
                await _messageSender.SendMessageAsync(vkUserId, "⚠️ Сначала войди: /login");
                return;
            }

            // Старт интерактивного диалога
            var session = new BotSession
            {
                CommandName = CommandName,
                Step = 1,
                Data = new Dictionary<string, string>()
            };
            _sessionStore.Set(vkUserId, session);

            await _messageSender.SendMessageAsync(vkUserId,
                "**Добавление виртуальной тренировки**\n\n" +
                "Выбери тип тренировки (введи цифру):\n" +
                "1️ Бег\n" +
                "2️ Велосипед\n" +
                "3️ Лыжи\n" +
                "4️ Другое\n\n" +
                "Для отмены: /cancel");
        }

        public async Task HandleStepAsync(long vkUserId, BotSession session, string input)
        {
            switch (session.Step)
            {
                case 1: // Тип тренировки
                    if (!HandleSportType(session, input))
                    {
                        await _messageSender.SendMessageAsync(vkUserId,
                            "Введи цифру от 1 до 4");
                        return;
                    }
                    session.Step = 2;
                    _sessionStore.Set(vkUserId, session);
                    await _messageSender.SendMessageAsync(vkUserId,
                        "Введи дистанцию в метрах (например: 10000)");
                    break;

                case 2: // Дистанция
                    if (!uint.TryParse(input, out uint distance) || distance == 0)
                    {
                        await _messageSender.SendMessageAsync(vkUserId,
                            "Введи число метров (например: 10000)");
                        return;
                    }
                    session.Data["distance"] = distance.ToString();
                    session.Step = 3;
                    _sessionStore.Set(vkUserId, session);
                    await _messageSender.SendMessageAsync(vkUserId,
                        "Введи длительность в секундах (например: 3600 = 1 час)");
                    break;

                case 3: // Длительность
                    if (!double.TryParse(input, out double duration) || duration <= 0)
                    {
                        await _messageSender.SendMessageAsync(vkUserId,
                            "Введи число секунд (например: 3600)");
                        return;
                    }
                    session.Data["duration"] = duration.ToString();
                    session.Step = 4;
                    _sessionStore.Set(vkUserId, session);
                    await _messageSender.SendMessageAsync(vkUserId,
                        "Введи средний пульс (например: 145) или 0 чтобы пропустить");
                    break;

                case 4: // Пульс
                    if (!byte.TryParse(input, out byte hr))
                    {
                        await _messageSender.SendMessageAsync(vkUserId,
                            "Введи число от 0 до 255");
                        return;
                    }
                    session.Data["hr"] = hr.ToString();
                    session.Step = 5;
                    _sessionStore.Set(vkUserId, session);
                    await _messageSender.SendMessageAsync(vkUserId,
                        "Введи описание (или '-' чтобы пропустить)");
                    break;

                case 5: // Описание → сохраняем
                    var description = input.Trim();
                    if (description == "-") description = "";
                    session.Data["description"] = description;

                    await SaveWorkoutAsync(vkUserId, session);
                    _sessionStore.Remove(vkUserId);
                    break;
            }
        }

        private static bool HandleSportType(BotSession session, string input)
        {
            var (valid, sportType) = input.Trim() switch
            {
                "1" => (true, "running"),
                "2" => (true, "cycling"),
                "3" => (true, "skiing"),
                "4" => (true, "other"),
                _ => (false, "")
            };

            if (valid)
                session.Data["sportType"] = sportType;

            return valid;
        }

        private async Task SaveWorkoutAsync(long vkUserId, BotSession session)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.VkId == vkUserId);
            if (user == null)
            {
                await _messageSender.SendMessageAsync(vkUserId, "❌ Пользователь не найден");
                return;
            }

            // Получаем или создаём виртуальное устройство
            var device = await _context.Devices
                .FirstOrDefaultAsync(d => d.UserId == user.UserId && d.IsVirtual);

            if (device == null)
            {
                device = new Device
                {
                    SerialNumber = 0,
                    UserId = user.UserId,
                    IsVirtual = true,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Devices.Add(device);
                await _context.SaveChangesAsync();
            }

            var distance = uint.Parse(session.Data["distance"]);
            var duration = double.Parse(session.Data["duration"]);
            var hr = byte.Parse(session.Data["hr"]);
            var description = session.Data["description"];
            var sportType = session.Data["sportType"];

            var now = DateTime.UtcNow;
            var timeStart = now.AddSeconds(-duration);

            Workout workout = sportType switch
            {
                "running" => new WorkoutRunning(),
                "cycling" => new WorkoutCycling(),
                "skiing" => new WorkoutSkiing(),
                _ => new WorkoutOther()
            };

            workout.DeviceId = device.DeviceId;
            workout.Device = device;
            workout.TimeStart = timeStart;
            workout.TimeEnd = now;
            workout.DistanceMeters = distance;
            workout.AvgHeartRate = hr > 0 ? hr : null;
            workout.Description = description;
            workout.SerialNumberDevice = 0;
            workout.SetIdentifier();

            _context.Workouts.Add(workout);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "VK user {VkId} добавил виртуальную тренировку {WorkoutId} типа {Type}",
                vkUserId, workout.WorkoutId, sportType);

            var sportName = sportType switch
            {
                "running" => "Бег",
                "cycling" => "Велосипед",
                "skiing" => "Лыжи",
                _ => "Другое"
            };

            await _messageSender.SendMessageAsync(vkUserId,
                $"**Тренировка добавлена!**\n\n" +
                $"{sportName}\n" +
                $"Дистанция: {distance / 1000.0:F2} км\n" +
                $"Длительность: {FormatDuration(TimeSpan.FromSeconds(duration))}\n" +
                $"Пульс: {(hr > 0 ? hr.ToString() : "—")}\n" +
                $"{timeStart.ToLocalTime():dd.MM.yyyy HH:mm}\n\n" +
                $"Чтобы добавить ещё: /workout");
        }

        private static string FormatDuration(TimeSpan ts)
        {
            if (ts.TotalHours < 1) return $"{ts.Minutes} мин";
            if (ts.TotalDays < 1) return $"{(int)ts.TotalHours} ч {ts.Minutes} мин";
            return $"{(int)ts.TotalDays} д {(int)ts.TotalHours % 24} ч";
        }
    }
}
