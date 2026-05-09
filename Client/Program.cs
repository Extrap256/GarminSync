using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Client.Core.Interfaces;
using Client.Services;
using Client.Storage;
using Client.UI;

namespace Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // 1. Загрузка конфигурации
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // 2. Настройка DI-контейнера
            var services = new ServiceCollection();
            ConfigureServices(services, configuration);
            using var serviceProvider = services.BuildServiceProvider();

            // 3. Обработка отмены (Ctrl + C)
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            // 4. Запуск основного сценария
            try
            {
                await RunWorkflowAsync(serviceProvider, cts.Token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("\nОперация отменена пользователем.");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nКритическая ошибка: {ex.Message}");
                Console.ResetColor();
#if DEBUG
                Console.WriteLine(ex.StackTrace);
#endif
            }
            finally
            {
                Console.WriteLine("\nНажмите любую клавишу для выхода...");
                Console.ReadKey(true);
            }
        }

        /// <summary>
        /// Регистрация всех зависимостей
        /// </summary>
        private static void ConfigureServices(IServiceCollection services, IConfiguration config)
        {
            // База данных (SQLite)
            var connStr = config.GetSection("Database:ConnectionString").Value
                          ?? "Data Source=garmin_sync_local.db";
            services.AddDbContext<LocalDataBaseContext>(options =>
                options.UseSqlite(connStr));

            // Путь к папке активностей
            var actPath = config.GetSection("Device:ActivitiesRelativePath").Value
                          ?? @"GARMIN\ACTIVITIES";

            // Сервисы
            services.AddTransient<IDeviceScanner>(_ => new DeviceScanner(actPath));
            services.AddTransient<IFitParser, FitParser>();
            services.AddScoped<IActivityRepository, ActivityRepository>();
            services.AddScoped<ISyncOrchestrator, Orchestrator>();

            // UI
            services.AddTransient<IAppUI, ConsoleUI>();
        }

        /// <summary>
        /// Основной поток выполнения приложения
        /// </summary>
        private static async Task RunWorkflowAsync(IServiceProvider provider, CancellationToken ct)
        {
            // Создаём scope для Scoped-сервисов (DbContext, Repository, Orchestrator)
            using var scope = provider.CreateScope();
            var sp = scope.ServiceProvider;

            var ui = sp.GetRequiredService<IAppUI>();
            var scanner = sp.GetRequiredService<IDeviceScanner>();
            var orchestrator = sp.GetRequiredService<ISyncOrchestrator>();

            ui.ShowHeader();
            ui.ShowMessage("Сканирование подключенных устройств...");

            var devices = await scanner.ScanAsync(ct);
            ct.ThrowIfCancellationRequested();

            var selected = await ui.SelectDeviceAsync(devices, ct);
            if (selected is null) return;
            ct.ThrowIfCancellationRequested();

            ui.ShowMessage($"\nЗапуск синхронизации: {selected.Label}");
            ui.ShowMessage($"Путь: {selected.ActivitiesPath}\n");

            var result = await orchestrator.RunAsync(selected.ActivitiesPath, ct);

            // Вывод итогов
            ui.ShowMessage("\n=================================");
            ui.ShowMessage($"Сохранено: {result.Processed}");
            if (result.Skipped > 0)
                ui.ShowMessage($"Пропущено (дубликаты): {result.Skipped}");

            if (result.Errors.Count > 0)
            {
                ui.ShowError($"Ошибок: {result.Errors.Count}");
                foreach (var err in result.Errors.Take(5))
                    Console.WriteLine($"   • {err}");
                if (result.Errors.Count > 5)
                    Console.WriteLine($"   ... и ещё {result.Errors.Count - 5}");
            }
            ui.ShowMessage("=================================");
        }
    }
}