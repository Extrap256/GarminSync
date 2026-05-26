using System.Globalization;
using Client.Core.Interfaces;
using Client.Core.Interfaces.DTO;
using Client.Core.Interfaces.DTO.API;
using Client.Core.Interfaces.Enums;
using EntityClassLibraryClient.Models;

namespace Client.UI
{
    public class ConsoleUI (ILocalRepository repository,
        IWorkoutSyncService syncService,
        IReportService reportService,
        IUserService userService) : IAppUI
    {
        private const int ProgressBarWidth = 40;

        private readonly ILocalRepository repository = repository;
        private readonly IWorkoutSyncService syncService = syncService;
        private readonly IReportService reportService = reportService;
        private readonly IUserService userService = userService;

        public async Task AddVirtualWorkoutAsync(CancellationToken ct = default)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Новая тренировка");
            Console.ResetColor();
            Console.WriteLine();

            try
            {
                Console.WriteLine("Тип тренировки:");
                Console.WriteLine("1. Бег");
                Console.WriteLine("2. Велосипед");
                Console.WriteLine("3. Лыжи");
                Console.WriteLine("4. Другое");
                Console.WriteLine("0. Отмена");

                Console.Write("Выбор: ");
                var typeInput = Console.ReadLine()?.Trim();

                if (typeInput == "0")
                    return;

                Console.Write("Время начала (dd.MM.YYYY HH:mm): ");
                if (!DateTime.TryParse(Console.ReadLine(), out DateTime timeStart))
                {
                    ShowError("Некорректная дата");
                    return;
                }

                Console.Write("Время конца (dd.MM.YYYY HH:mm): ");
                if (!DateTime.TryParse(Console.ReadLine(), out DateTime timeEnd) || timeEnd <= timeStart)
                {
                    ShowError("Время конца должно быть позже начала");
                    return;
                }

                Console.Write("Дистанция (м, Enter - пропустить): ");
                uint? distance = uint.TryParse(Console.ReadLine(), out uint d) ? d : null;

                Console.Write("Средний пульс (уд/мин, Enter - пропустить): ");
                byte? hr = byte.TryParse(Console.ReadLine(), out byte h) ? h : null;

                Console.Write("Набор высоты (м, Enter — пропустить): ");
                int? elev = int.TryParse(Console.ReadLine(), out int e) ? e : null;

                Console.Write("Описание (Enter — пропустить): ");
                var description = Console.ReadLine();

                var virtualDevice = await repository.GetOrCreateVirtualDeviceAsync(ct);

                Workout workout = typeInput switch
                {
                    "1" => new WorkoutRunning(),
                    "2" => new WorkoutCycling(),
                    "3" => new WorkoutSkiing(),
                    _ => new WorkoutOther()
                };

                workout.Device = virtualDevice;
                workout.DeviceId = virtualDevice.DeviceId;
                workout.TimeStart = timeStart;
                workout.TimeEnd = timeEnd;
                workout.DistanceMeters = distance;
                workout.AvgHeartRate = hr;
                workout.ElevationGain = elev;
                workout.Description = description;

                workout.SetIdentifier();

                if (await repository.ExistByIdentifierAsync(workout.Identifier, ct))
                {
                    ShowError($"Тренировка на это время уже существует");
                    WaitForContinue();
                    return;
                }

                await repository.AddAsync(workout, null, ct);
                await repository.SaveChangeAsync(ct);

                ShowMessage($"Тренировка #{workout.WorkoutId} добавлена (ID: {workout.Identifier + 1})");
                WaitForContinue();
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка: {ex.Message}");
                WaitForContinue();
            }
        }
        public async Task WorkoutsMenuAsync(CancellationToken ct = default)
        {
            while (!ct.IsCancellationRequested)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("Мои тренировки");
                Console.ResetColor();
                Console.WriteLine();

                var workouts = await repository.GetAllWorkoutsAsync(ct);

                if (workouts.Count == 0)
                {
                    ShowMessage("Нет тренировок");
                    WaitForContinue();
                    return;
                }

                Console.WriteLine($"{"ID",4} │ {"Дата",10} │ {"Тип",15} │ {"Дистанция",10} │ {"Синхр",5}");
                Console.WriteLine(new string('-', 55));

                foreach (var workout in workouts.Take(30))
                {
                    var type = workout.GetType().Name.Replace("Workout", "");
                    var dist = workout.DistanceMeters.HasValue ? $"{workout.DistanceMeters / 1000.0:F2} км" : "-";
                    var sync = workout.IsSynced ? "Да" : "Нет";
                    Console.WriteLine($"{workout.WorkoutId + 1,4} │ {workout.TimeStart:yyyy.MM.dd} │ {type,-15} │ {dist,10} │ {sync,5}");
                }

                if (workouts.Count > 20)
                    Console.WriteLine($"... и еще {workouts.Count - 20}");

                Console.WriteLine("Введите ID тренировки для редактирования (0 - назад)");
                var input = Console.ReadLine()?.Trim();

                if (input == "0" || string.IsNullOrEmpty(input))
                    return;

                if (int.TryParse(input, out int id))
                {
                    await EditWorkoutAsync(id, ct);
                }
            }
        }

        public async Task EditWorkoutAsync(int workoutId, CancellationToken ct)
        {
            var workout = await repository.GetWorkoutByIdAsync(workoutId, ct);
            if (workout == null)
            {
                ShowError($"Тренировка #{workoutId + 1} не найдена");
                WaitForContinue();
                return;
            }

            while (!ct.IsCancellationRequested)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Редактирование тренировки");
                Console.ResetColor();
                Console.WriteLine();

                Console.WriteLine($"1. Описание:      {workout.Description ?? "—"}");
                Console.WriteLine($"2. Дистанция:     {workout.DistanceMeters?.ToString() ?? "—"} м");
                Console.WriteLine($"3. Ср. пульс:     {workout.AvgHeartRate?.ToString() ?? "—"} уд/мин");
                Console.WriteLine($"4. Набор высоты:  {workout.ElevationGain?.ToString() ?? "—"} м");
                Console.WriteLine($"5. Время начала:  {workout.TimeStart:dd.MM.yyyy HH:mm}");
                Console.WriteLine($"6. Общее время:   {workout.TimeEnd:dd.MM.yyyy HH:mm}");
                Console.WriteLine($"\nСинхронизировано: {(workout.IsSynced ? "Да" : "Нет")}");
                Console.WriteLine();
                Console.WriteLine("Выберите поле для изменения (s — сохранить, 0 — отмена): ");

                var choice = Console.ReadLine()?.Trim().ToLower();

                try
                {
                    switch (choice)
                    {
                        case "1":
                            Console.Write("Новое описание: ");
                            workout.Description = Console.ReadLine();
                            break;
                        case "2":
                            Console.Write("Новая дистанция (м): ");
                            if (uint.TryParse(Console.ReadLine(), out uint dist))
                                workout.DistanceMeters = dist;
                            break;
                        case "3":
                            Console.Write("Новая средний пульс (уд/мин): ");
                            if (byte.TryParse(Console.ReadLine(), out byte hr))
                                workout.AvgHeartRate = hr;
                            break;
                        case "4":
                            Console.Write("Новый набор высоты (м): ");
                            if (int.TryParse(Console.ReadLine(), out int elev))
                                workout.ElevationGain = elev;
                            break;
                        case "5":
                            Console.Write("Новое время начала (dd.MM.yyyy HH:mm): ");
                            if (DateTime.TryParse(Console.ReadLine()?.Trim(), out DateTime start))
                                workout.TimeStart = start;
                            break;
                        // Сделать
                        //case "6":
                        //    Console.Write("Новое общее время (HH:mm): ");
                        //    if (TimeSpan.TryParse(Console.ReadLine(), out TimeSpan dist))
                        //        workout.DistanceMeters = dist;
                        //    break;
                        case "s":
                            if (workout.TimeEnd <= workout.TimeStart)
                            {
                                ShowError("Время конца должно быть позже начала");
                                WaitForContinue();
                                continue;
                            }
                            await repository.UpdateWorkoutAsync(workout, ct);
                            await repository.SaveChangeAsync(ct);
                            ShowMessage("Сохранено. Тренировка помечена как несинхронизированная");
                            WaitForContinue();
                            return;
                        case "0":
                            return;
                    }
                }
                catch (Exception ex)
                {
                    ShowError($"Ошибка: {ex.Message}");
                    WaitForContinue();
                }
            }
        }

        public async Task SyncMenuAsync(CancellationToken ct = default)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Синхронизация с сервером");
            Console.ResetColor();
            Console.WriteLine();

            try
            {
                ShowMessage("Отправка несинхронизированных тренировок...");
                var count = await syncService.SyncAllUnsyncedAsync(ct);

                ShowMessage($"\nСинхронизировано: {count} тренировок");
            }
            catch (UnauthorizedAccessException)
            {
                ShowError("Сессия истекла. Войдите заново.");
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка синхронизации: {ex.Message}");
            }

            WaitForContinue();
        }

        public async Task ReportMenuAsync(CancellationToken ct = default)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Отчеты");
            Console.ResetColor();
            Console.WriteLine();

            Console.Write("Дата начала (дд.мм.гггг): ");
            var fromStr = Console.ReadLine();

            Console.Write("Дата окончания (дд.мм.гггг): ");
            var toStr = Console.ReadLine();

            if (!DateTime.TryParse(fromStr, new CultureInfo("ru-RU"), out var from) || !DateTime.TryParse(toStr, new CultureInfo("ru-RU"), out var to))
            {
                ShowError("Некорректный формат даты");
                WaitForContinue();
                return;
            }

            try
            {
                var request = new PersonalReportRequest(from.Date, to.Date.AddDays(1).AddSeconds(-1));
                var report = await reportService.GetPersonalReportAsync(request, ct);

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(report.Content);
                Console.ResetColor();
            }
            catch (UnauthorizedAccessException)
            {
                ShowError("Сессия истекла");
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка получения отчета: {ex.Message}");
            }

            WaitForContinue();
        }

        /// <summary>
        /// Работа с профилем (вход, регистрация, работа локально)
        /// </summary>
        public async Task<AuthAction> ShowAuthMenuAsync(CancellationToken ct = default)
        {
            AuthAction? result = null;
            while (!ct.IsCancellationRequested)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Авторизация");
                Console.ResetColor();
                Console.WriteLine();

                Console.WriteLine("0. Выход из программы");
                Console.WriteLine("1. Войти в аккаунт");
                Console.WriteLine("2. Регистрация");
                Console.WriteLine("3. Пропустить (локальный режим)");
                Console.Write("\nВыбор: ");

                var input = Console.ReadLine()?.Trim();
                result = input switch
                {
                    "1" => AuthAction.Login,
                    "2" => AuthAction.Register,
                    "3" => AuthAction.Skip,
                    "0" => AuthAction.Exit,

                    _ => null
                };
                if (result == null)
                {
                    Console.WriteLine("Неверный выбор\n\nВыберите из списка");
                    await Task.Delay(2000, ct);
                }
                else
                    break;
            }
            return result!.Value;
        }

        /// <summary>
        /// Вывод профиля с возможностью изменения
        /// </summary>
        public async Task ProfileMenuAsync(CancellationToken ct = default)
        {
            while (!ct.IsCancellationRequested)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Профиль");
                Console.ResetColor();
                Console.WriteLine();

                try
                {
                    var user = await userService.GetCurrentUserAsync(ct);

                    Console.WriteLine($"Логин: {user.Login}");
                    Console.WriteLine($"Имя: {user.FirstName}");
                    Console.WriteLine($"Фамилия: {user.LastName}");
                    Console.WriteLine($"Год рождения: {user.YearOfBirth}");
                    Console.WriteLine($"Регистрация: {user.CreatedAt:dd-MM-yyyy}");
                    Console.WriteLine();
                }
                catch (UnauthorizedAccessException)
                {
                    ShowMessage("Сессия истекла. Войдите заново");
                }
                catch (Exception ex)
                {
                    ShowMessage($"Не удалось загрузить провиль: {ex.Message}");
                }

                Console.WriteLine("1. Изменить имя");
                Console.WriteLine("2. Изменить фамилию");
                Console.WriteLine("3. Изменить год рождения");
                Console.WriteLine("0. Назад");
                Console.WriteLine("Выбор: ");

                var input = Console.ReadLine()?.Trim();

                try
                {
                    var request = new UpdateUserRequest();

                    switch (input)
                    {
                        case "1":
                            Console.Write("Новое имя: ");
                            request.FirstName = Console.ReadLine();
                            break;
                        case "2":
                            Console.Write("Новая фамилия: ");
                            request.LastName = Console.ReadLine();
                            break;
                        case "3":
                            Console.Write("Новый год рождения: ");
                            if (int.TryParse(Console.ReadLine(), out int year))
                                request.YearOfBirth = year;
                            else
                            {
                                ShowMessage("Некорректный год");
                                await Task.Delay(1000, ct);
                                continue;
                            }
                            break;
                        case "0":
                            return;
                        default:
                            continue;
                    }

                    if (!request.HasChanges)
                        continue;

                    await userService.UpdateCurrentUserAsync(request, ct);
                    ShowMessage("Профиль успешно обновлен");
                    await Task.Delay(1000, ct);
                }
                catch (UnauthorizedAccessException)
                {
                    ShowMessage("Сессия истекла. Войдите заново");
                }
                catch (Exception ex)
                {
                    ShowMessage($"Ошибка обновления: {ex.Message}");
                    await Task.Delay(2000, ct);
                }
            }
        }

        /// <summary>
        /// Метод для входа пользователя в систему
        /// </summary>
        /// <returns>Запрос с данными для входа</returns>
        public async Task<LoginRequest> PromptLoginAsync(CancellationToken ct = default)
        {
            Console.Write("Логин: ");
            var login = Console.ReadLine();

            Console.Write("Пароль: ");
            var password = ReadPassword();

            return new LoginRequest(login ?? "", password);
        }

        private static string ReadPassword()
        {
            var password = new System.Text.StringBuilder();
            while (true)
            {
                var key = Console.ReadKey(intercept: true);
                if (key.Key == ConsoleKey.Enter)
                    break;

                if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password.Remove(password.Length - 1, 1);
                    Console.Write("\b \b");
                }
                else
                    if (!char.IsControl(key.KeyChar))
                    {
                        password.Append(key.KeyChar);
                        Console.Write("*");
                    }
            }
            Console.WriteLine();
            return password.ToString();
        }

        /// <summary>
        /// Метод для регистрации нового пользователя
        /// </summary>
        /// <returns>Запрос с даннными регистрации</returns>
        public async Task<RegisterRequest> PromptRegisterAsync(CancellationToken ct = default)
        {
            Console.Write("Логин: ");
            var login = Console.ReadLine();

            Console.Write("Пароль: ");
            var password = ReadPassword();

            Console.Write("Имя: ");
            var firstName = Console.ReadLine();

            Console.Write("Фамилия: ");
            var lastName = Console.ReadLine();

            Console.Write("Год рождения: ");
            string? yearInput = Console.ReadLine();
            bool isSuccess = int.TryParse(yearInput, out int yearOfBirth);

            return new RegisterRequest(
                login ?? "",
                password ?? "",
                firstName ?? "",
                lastName ?? "",
                yearOfBirth);
        }

        /// <summary>
        /// Показывает список устройств и запрашивает выбор у пользователя
        /// </summary>
        public async Task<DeviceInfo?> SelectDeviceAsync(IReadOnlyList<DeviceInfo> devices, CancellationToken ct = default)
        {
            if (devices == null || devices.Count == 0)
            {
                ShowMessage("Устройства не найдены. Подключите часы Garmin и попробуйте снова.");
                return null;
            }

            ShowMessage($"\nНайдено устройств: {devices.Count}");
            ShowMessage(new string('─', 50));

            for (int i = 0; i < devices.Count; i++)
            {
                var device = devices[i];
                Console.WriteLine($"\t[{i + 1}] {device.Label}");
                Console.WriteLine($"\tПуть: {device.ActivitiesPath}");

                if (ct.IsCancellationRequested)
                    return null;

                await Task.Delay(50, ct); // задержка для плавности вывода
            }

            ShowMessage(new string('─', 50));
            Console.Write("\nВыберите устройство (введите номер или 0 для выхода): ");

            while (!ct.IsCancellationRequested)
            {
                var input = Console.ReadLine()?.Trim();

                if (string.IsNullOrWhiteSpace(input))
                    continue;

                if (input == "0")
                {
                    ShowMessage("Операция отменена пользователем");
                    return null;
                }

                if (int.TryParse(input, out int selected) && selected >= 1)
                {
                    var chosen = devices[selected - 1];
                    ShowMessage($"\nВыбрано: {chosen.Label}");
                    return chosen;
                }

                Console.Write("❌ Неверный номер. Попробуйте снова: ");
            }

            return null;
        }

        /// <summary>
        /// Выводит информационное сообщение
        /// </summary>
        public void ShowMessage(string message)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(message);
            Console.ForegroundColor = originalColor;
        }

        /// <summary>
        /// Выводит сообщение об ошибке красным цветом
        /// </summary>
        public void ShowError(string message)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{message}");
            Console.ForegroundColor = originalColor;
        }

        /// <summary>
        /// Показывает прогресс обработки файлов
        /// </summary>
        public void ShowProgress(string fileName, int current, int total)
        {
            if (total <= 0)
                return;

            var percent = (int)((double)current / total * 100);
            var filled = (int)((double)current / total * ProgressBarWidth);

            // Возвращаем курсор в начало строки для обновления прогресса
            Console.SetCursorPosition(0, Console.CursorTop);

            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Cyan;

            Console.Write($"[{new string('█', filled)}{new string('░', ProgressBarWidth - filled)}] ");
            Console.Write($"{percent,3}% ");
            Console.Write($"[{current}/{total}] ");
            Console.Write($"{Path.GetFileName(fileName),-30}");

            Console.ForegroundColor = originalColor;

            // Если это последний файл — переходим на новую строку
            if (current >= total)
                Console.WriteLine();
        }

        /// <summary>
        /// Очищает консоль и выводит заголовок приложения
        /// </summary>
        public void ShowHeader()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Garmin Sync — Консольная версия");
            Console.ResetColor();
            Console.WriteLine();
        }

        /// <summary>
        /// Запрашивает подтверждение действия
        /// </summary>
        public bool Confirm(string question)
        {
            Console.Write($"\n{question} (Y/N): ");
            var key = Console.ReadKey(intercept: true);
            Console.WriteLine(key.KeyChar);

            if (key.Key == ConsoleKey.Enter)
                return true;
            return key.KeyChar is 'Y' or 'y' or 'Д' or 'д';
        }

        /// <summary>
        /// Ждёт нажатия любой клавиши для продолжения
        /// </summary>
        public void WaitForContinue()
        {
            Console.WriteLine("\nНажмите любую клавишу для продолжения...");
            Console.ReadKey(intercept: true);
        }
    }
}