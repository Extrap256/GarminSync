using Client.Core.Interfaces;
using Client.Core.Interfaces.DTO;
using Client.Infrastructure.Storage;
using EntityClassLibraryClient.Models;
using MediaDevices;


namespace Client.Services
{
    public class Orchestrator : ISyncOrchestrator
    {
        private readonly IFitParser _fitParser;
        private readonly ILocalRepository _localRepository;
        private readonly IAppUI _ui;

        public Orchestrator(IFitParser fitParser, ILocalRepository activityRepository, IAppUI appUI)
        {
            _fitParser = fitParser;
            _localRepository = activityRepository;
            _ui = appUI;
        }

        public async Task<SyncResult> RunAsync(DeviceInfo deviceInfo, CancellationToken ct = default)
        {
            if (deviceInfo == null)
                throw new ArgumentNullException(nameof(deviceInfo));

            if (deviceInfo.IsMtpDevice && deviceInfo.MtpDevice != null)
                deviceInfo.MtpDevice.Connect();

            IReadOnlyList<string> fitFiles;

            if (deviceInfo.IsMtpDevice && deviceInfo.MtpDevice != null)
            {
                fitFiles = await GetMtpFitFilesAsync(deviceInfo, ct);
            }
            else
            {
                fitFiles = Directory.GetFiles(deviceInfo.ActivitiesPath, "*.fit", SearchOption.TopDirectoryOnly);
            }

            if (fitFiles.Count == 0)
            {
                _ui.ShowMessage("Файлы .fit не найдены");
                return new SyncResult(0, 0, Array.Empty<string>());
            }

            _ui.ShowMessage($"Найдено файлов: {fitFiles.Count}");

            DeviceData deviceData = await _fitParser.ParseDeviceAsync(deviceInfo.DeviceInfoPath, deviceInfo, ct);

            int processed = 0;
            int skipped = 0;
            List<string> errors = new();

            for (int i = 0; i < fitFiles.Count; i++)
            {
                if (ct.IsCancellationRequested)
                {
                    _ui.ShowMessage("Отмена пользователем");
                    break;
                }

                string filePath = fitFiles[i];

                try
                {
                    _ui.ShowProgress(filePath, i + 1, fitFiles.Count);

                    FitDataCollector dto;

                    if (deviceInfo.IsMtpDevice && deviceInfo.MtpDevice != null)
                    {
                        dto = await _fitParser.ParseActivityAsync(filePath, deviceInfo, ct);
                    }
                    else
                    {
                        dto = await _fitParser.ParseActivityAsync(filePath, ct: ct);
                    }

                    string identifier = dto.GetIdentifier();

                    if (await _localRepository.ExistByIdentifierAsync(identifier, ct))
                    {
                        skipped++;
                        continue;
                    }

                    Workout workout = ConvertDtoToEntity.FitDataCollectorToWorkout(dto);
                    workout.Device = await _localRepository.GetOrCreateDeviceAsync(dto.DeviceData.SerialNumber);

                    await _localRepository.AddAsync(workout, deviceData.SerialNumber, ct);
                    processed++;
                }
                catch (OperationCanceledException)
                {
                    // Пользователь затребовал
                    break;
                }
                catch (Exception ex)
                {
                    errors.Add($"{filePath}: {ex.Message}_{ex.InnerException?.Message}");
                }
            }


            if (processed > 0)
            {
                try
                {
                    await _localRepository.SaveChangeAsync(ct);
                    _ui.ShowMessage($"В локальную базу сохранено тренировок: {processed}");
                }
                catch (Exception ex)
                {
                    errors.Add($"Ошибка сохранения в БД: {ex.Message}{"_" + ex.InnerException?.Message ?? "null"}");
                }
            }

            if (deviceInfo.IsMtpDevice && deviceInfo.MtpDevice != null)
            {
                deviceInfo.MtpDevice.Disconnect();
                deviceInfo.MtpDevice.Dispose();
            }
            return new SyncResult(processed, skipped, errors);
        }

        private static async Task<IReadOnlyList<string>> GetMtpFitFilesAsync(
            DeviceInfo deviceInfo,
            CancellationToken ct)
        {
            var files = new List<string>();

            if (deviceInfo.MtpDevice == null)
                throw new InvalidOperationException("MTP-устройство не подключено");

            try
            {
                var root = deviceInfo.MtpDevice.GetRootDirectory();
                files = deviceInfo.MtpDevice.GetFiles(deviceInfo.ActivitiesPath, ".fit").ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка чтения MTP-устройства: {ex.Message}", ex);
            }

            return files;
        }
    }
}
