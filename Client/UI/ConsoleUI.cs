using System.Globalization;
using Client.Core.Interfaces;
using Client.Core.Interfaces.DTO;
using Client.Core.Interfaces.DTO.API;
using Client.Core.Interfaces.Enums;

namespace Client.UI
{
    public class ConsoleUI : IAppUI
    {
        private const int ProgressBarWidth = 40;

        public async Task SyncMenuAsync(
            IWorkoutSyncService syncService,
            IAppUI ui,
            CancellationToken ct = default)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Синхронизация с сервером");
            Console.ResetColor();
            Console.WriteLine();

            try
            {
                ui.ShowMessage("Отправка несинхронизированных тренировок...");
                var count = await syncService.SyncAllUnsyncedAsync(ct);

                ui.ShowMessage($"\nСинхронизировано: {count} тренировок");
            }
            catch (UnauthorizedAccessException)
            {
                ui.ShowError("Сессия истекла. Войдите заново.");
            }
            catch (Exception ex)
            {
                ui.ShowError($"Ошибка синхронизации: {ex.Message}");
            }

            ui.WaitForContinue();
        }

        public async Task ReportMenuAsync(
            IReportService reportService,
            IAppUI ui,
            CancellationToken ct = default)
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
                ui.ShowError("Некорректный формат даты");
                ui.WaitForContinue();
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
                ui.ShowError("Сессия истекла");
            }
            catch (Exception ex)
            {
                ui.ShowError($"Ошибка получения отчета: {ex.Message}");
            }

            ui.WaitForContinue();
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
        public async Task ProfileMenuAsync(IUserService userService, CancellationToken ct = default)
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