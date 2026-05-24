using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Client.Core.Interfaces;
using Client.Services;
using Client.UI;
using Client.Core.Interfaces.DTO;
using Client.Infrastructure.Storage;
using Client.Infrastructure.API;
using Client.Infrastructure.Security.Interfaces;
using Client.Infrastructure.Security;
using Client.Core.Interfaces.Enums;

namespace Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // Загрузка конфигурации
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Настройка DI-контейнера
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

            // Запуск основного сценария
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

            var apiSection = config.GetSection(ApiSettings.SectionName);
            if (!apiSection.Exists() || string.IsNullOrEmpty(apiSection["BaseUrl"]))
                throw new InvalidOperationException($"Ошибка конфигурации: секция \"{ApiSettings.SectionName}\" отсутствует");

            services.Configure<ApiSettings>(apiSection);

            string baseUrl = apiSection["BaseUrl"]!;

            // Путь к папке активностей
            var actPath = config.GetSection("Device:ActivitiesRelativePath").Value
                          ?? @"GARMIN\ACTIVITIES";

            var devPath = config.GetSection("Device:DeviceInfoRelativePath").Value
                          ?? @"GARMIN\Device.fit";

            // Сервисы
            services.AddHttpClient("GarminApi", client =>
            {
                client.BaseAddress = new Uri(baseUrl);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            }).AddHttpMessageHandler<AuthHeaderHandler>();

            services.AddTransient<AuthHeaderHandler>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddSingleton<ITokenStorage, TokenStorage>(_ => new TokenStorage(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)));
            services.AddScoped<IWorkoutSyncService, WorkoutSyncService>();
            services.AddScoped<IReportService, ReportService>();

            services.AddSingleton<IDeviceScanner>(_ => new DeviceScanner(actPath, devPath));
            services.AddScoped<IFitParser, FitParser>();
            services.AddScoped<ILocalRepository, LocalRepository>();
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
            var authService = sp.GetRequiredService<IAuthService>();
            var userService = sp.GetRequiredService<IUserService>();
            var scanner = sp.GetRequiredService<IDeviceScanner>();
            var orchestrator = sp.GetRequiredService<ISyncOrchestrator>();
            var syncService = sp.GetRequiredService<IWorkoutSyncService>();
            var reportService = sp.GetRequiredService<IReportService>();

            ui.ShowHeader();

            // Выбор работы при входе
            var auth = await Authorization(authService, ui, ct);
            if (auth == AuthAction.Exit)
                return;

            while (!ct.IsCancellationRequested)
            {
                bool continueRunning = authService.IsAuthenticated()
                    ? await ShowMainMenuAsync(ui, authService, userService, scanner, orchestrator, syncService, reportService, ct)
                    : await ShowLimitedMenuAsync(ui, authService, scanner, orchestrator, ct);

                if (!continueRunning)
                    break;
            }
        }

        private static async Task<AuthAction> Authorization(
            IAuthService authService, 
            IAppUI ui, 
            CancellationToken ct = default)
        {
            if (authService.IsAuthenticated())
            {
                ui.ShowMessage("Вы уже авторизированы");
                return AuthAction.Skip;
            }

            var authAction = await ui.ShowAuthMenuAsync(ct);

            switch (authAction)
            {
                case AuthAction.Login:
                    try
                    {
                        var loginRequest = await ui.PromptLoginAsync(ct);
                        var loginResponse = await authService.LoginAsync(loginRequest, ct);
                        ui.ShowMessage($"Вход выполнен: {loginResponse.User.FirstName}");
                    }
                    catch (Exception ex)
                    {
                        ui.ShowError($"Ошибка входа: {ex.Message}");
                        ui.WaitForContinue();
                        return AuthAction.Skip;
                    }
                    break;

                case AuthAction.Register:

                    try
                    {
                        var reqRequest = await ui.PromptRegisterAsync(ct);
                        var regResponse = await authService.RegisterAsync(reqRequest, ct);
                        ui.ShowMessage($"Регистрация успешна: {regResponse.User.FirstName}");
                    }
                    catch (Exception ex)
                    {
                        ui.ShowError($"Ошибка регистрации: {ex.Message}");
                        ui.WaitForContinue();
                        return AuthAction.Skip;
                    }
                    break;

                case AuthAction.Skip:
                    ui.ShowMessage($"Работа в локальном режиме");
                    break;

                case AuthAction.Exit:
                    return AuthAction.Exit;
            }

            return authAction;
        }
        private static async Task<bool> ShowLimitedMenuAsync(
            IAppUI ui,
            IAuthService authService,
            IDeviceScanner scanner,
            ISyncOrchestrator orchestrator,
            CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Ограниченное почти главное меню");
                Console.ResetColor();
                Console.WriteLine();

                Console.WriteLine("1. Синхронизация с часов");
                Console.WriteLine("2. Авторизация");
                Console.WriteLine("0. Выход из программы");
                Console.Write("\nВыбор: ");

                var input = Console.ReadLine()?.Trim();

                switch (input)
                {
                    case "1":
                        await SyncFromDeviceAsync(ui, scanner, orchestrator, ct);
                        break;

                    case "2":
                        var result = await Authorization(authService, ui, ct);
                        if (result == AuthAction.Exit)
                            return false;
                        return true;

                    case "0":
                        return false;
                }
            }

            return false;
        }
        private static async Task<bool> ShowMainMenuAsync(
            IAppUI ui,
            IAuthService authService,
            IUserService userService,
            IDeviceScanner scanner,
            ISyncOrchestrator orchestrator,
            IWorkoutSyncService syncService,
            IReportService reportService,
            CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Главное меню");
                Console.ResetColor();
                Console.WriteLine();

                Console.WriteLine("1. Синхронизация с часов");
                Console.WriteLine("2. Мой профиль");
                Console.WriteLine("3. Отправить данные на сервер");
                Console.WriteLine("4. Получить отчет");
                Console.WriteLine("5. Выйти из аккаунта");

                Console.WriteLine("0. Выход из программы");
                Console.Write("\nВыбор: ");

                var input = Console.ReadLine()?.Trim();

                switch (input)
                {
                    case "1":
                        await SyncFromDeviceAsync(ui, scanner, orchestrator, ct);
                        break;

                    case "2":
                        await ui.ProfileMenuAsync(userService, ct);
                        break;

                    case "3" when authService.IsAuthenticated():
                        await ui.SyncMenuAsync(syncService, ui, ct);
                        break;

                    case "4":
                        await ui.ReportMenuAsync(reportService, ui, ct);
                        break;

                    case "5" when authService.IsAuthenticated():
                        await authService.LogoutAsync(ct);
                        ui.ShowMessage("Выход выполнен");
                        ui.WaitForContinue();
                        return true;

                    case "0":
                        return false;
                }
            }
            return false;
        }

        private static async Task SyncFromDeviceAsync(
            IAppUI ui,
            IDeviceScanner scanner,
            ISyncOrchestrator orchestrator,
            CancellationToken ct)
        {
            ui.ShowMessage("Сканирование подключенных устройств...");

            var devices = await scanner.ScanAsync(ct);
            ct.ThrowIfCancellationRequested();

            var selected = await ui.SelectDeviceAsync(devices, ct);
            if (selected is null)
                return;
            ct.ThrowIfCancellationRequested();

            ui.ShowMessage($"\nЗапуск синхронизации: {selected.Label}");
            ui.ShowMessage($"Путь: {selected.ActivitiesPath}\n");


            var result = await orchestrator.RunAsync(selected, ct);

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

            ui.WaitForContinue();
        }
    }
}