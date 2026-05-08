using Client.Core.Interfaces;
using Client.Core.Interfaces.DTO;
using Client.Storage;
using Client.Storage.Models;

namespace Client.Services
{
    public class Orchestrator : ISyncOrchestrator
    {
        private readonly IFitParser _fitParser;
        private readonly IActivityRepository _activityRepository;
        private readonly IAppUI _ui;

        public Orchestrator(IFitParser fitParser, IActivityRepository activityRepository, IAppUI appUI)
        {
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

                    Workout workout = ConvertDtoToEntity.FitDataCollectorToWorkout(dto);

                    await _activityRepository.AddAsync(workout, ct);
                    processed++;
                }
                catch (OperationCanceledException)
                {
                    // Пользователь затребовал
                    break;
                }
                catch (Exception ex)
                {
                    errors.Add($"{fileName}: {ex.Message}");
                }
            }

            if (processed > 0)
            {
                try
                {
                    await _activityRepository.SaveChangeAsync(ct);
                    _ui.ShowMessage($"В локальную базу сохранено тренировок: {processed}");
                }
                catch (Exception ex)
                {
                    errors.Add($"Ошибка сохранения в БД: {ex.Message}");
                }
            }

            return new SyncResult(processed, skipped, errors);
        }
    }
}
