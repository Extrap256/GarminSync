using Client.Core.Interfaces;
using Client.Core.Interfaces.DTO;

namespace Client.UI
{
    public class ConsoleUI : IAppUI
    {
        private const int ProgressBarWidth = 40;

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