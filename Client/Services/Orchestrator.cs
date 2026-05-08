using Client.Core.Interfaces;
using Client.Core.Interfaces.DTO;

namespace Client.Services
{
    public class Orchestrator : ISyncOrchestrator
    {
        private IDeviceScanner _deviceScanner;
        private IFitParser _fitParser;
        private IActivityRepository _activityRepository;
        private IAppUI _ui;

        public Orchestrator(IDeviceScanner deviceScanner, IFitParser fitParser, IActivityRepository activityRepository, IAppUI appUI)
        {
            _deviceScanner = deviceScanner;
            _fitParser = fitParser;
            _activityRepository = activityRepository;
            _ui = appUI;
        }

        public async Task<SyncResult> RunAsync(string activitiesFolderPath, CancellationToken ct = default)
        {
            if (Directory.Exists(activitiesFolderPath))
                throw new DirectoryNotFoundException(activitiesFolderPath);

            var fitsFilesPaths = Directory.GetFiles(activitiesFolderPath, "*.fit", SearchOption.TopDirectoryOnly);

            if (fitsFilesPaths.Length == 0)
            {
                _ui.ShowMessage("Файлы не найдены");
                return new SyncResult(0, 0, []);
            }

            _ui.ShowMessage($"Найдено файлов: {fitsFilesPaths.Length}");

            int processed = 0;
            int skipped = 0;
            List<string> errors = new();

            for (int i = 0; i < fitsFilesPaths.Length; i++)
            {
                if (ct.IsCancellationRequested)
                {
                    _ui.ShowMessage("Отмена пользователем");
                    break;
                }

                string filePath = fitsFilesPaths[i];
                string fileName = Path.GetFileName(filePath);

                try
                {
                    _ui.ShowProgress(fileName, i + 1, fitsFilesPaths.Length);

                    FitDataCollector dto = await _fitParser.ParseAsync(filePath, ct);

                    string identifier = dto.GetIdentifier();

                    if (await _activityRepository.ExistByIdentifierAsync(identifier ,ct))
                    {
                        skipped++;
                        continue;
                    }


                }
                catch (Exception)
                {

                    throw;
                }
            }

        }
    }
}
