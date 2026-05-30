using EntityClassLibraryServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Server.API.Models.Workout;
using Server.Common.Exceptions;
using Server.Core.Interfaces;
using Server.Infrastructure.Data;

namespace Server.Core.Services
{
    public class WorkoutSyncService (ApplicationDbContext context, ILogger<WorkoutSyncService> logger)
        : IWorkoutSyncService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly ILogger<WorkoutSyncService> _logger = logger;


        public async Task<int> SyncWorkoutAsync(int userId, WorkoutDto dto, CancellationToken ct = default)
        {
            var devices = await _context.Devices
                .Where(d => d.UserId == userId)
                .ToListAsync(ct);
            // Include()

            var existingIds = await _context.Workouts
                .Where(w => w.Identifier == dto.Identifier)
                .Select(w => w.Identifier)
                .ToListAsync(ct);
            var existingSet = existingIds.ToHashSet();

            return await SyncSingleInternalAsync(userId, dto, devices, existingSet, ct);
        }
        public async Task<WorkoutBatchSyncResponse> SyncWorkoutsBatchAsync(int userId, List<WorkoutDto> dtos, CancellationToken ct = default)
        {
            var response = new WorkoutBatchSyncResponse();

            //new
            if (dtos.Count == 0)
                return response;
            var devices = await _context.Devices
                .Where(d => d.UserId == userId)
                .ToListAsync(ct);

            var existingIds = dtos
                .Where(d => d.Identifier != null)
                .Select(d => d.Identifier)
                .Distinct()
                .ToList();
            var existingSet = existingIds.ToHashSet();

            _logger.LogInformation(
                "BatchSync: {Count} тренировок, {Devices} устройств, {Existing} существующих",
                dtos.Count, devices.Count, existingSet.Count);

            try
            {
                const int chunkSize = 20;

                for (int i = 0; i < dtos.Count; i+= chunkSize)
                {
                    var chunk = dtos.Skip(i).Take(chunkSize).ToList();

                    foreach (var dto in chunk)
                    {
                        try
                        {
                            var workoutId = await SyncSingleInternalAsync(userId, dto, devices, existingSet, ct);

                            response.SyncedIds.Add(workoutId);
                            response.Synced++;
                        }
                        catch (Exception ex)
                        {
                            response.Errors.Add(new BatchSyncError()
                            {
                                Identifer = dto.Identifier ?? "<null>",
                                Error = ex.Message
                            });

                            response.Failed++;
                            _logger.LogWarning("Ошибка синхронизации тренировки {Identifier} пользователя {UserId}", dto.Identifier, userId);
                        }
                    }

                    try
                    {
                        await _context.SaveChangesAsync(ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Ошибка SaveChange для чанка {Chunk}", i / chunkSize);
                        _context.ChangeTracker.Clear();
                    }

                    _context.ChangeTracker.Clear();
                }
            }
            finally
            {
                _context.ChangeTracker.AutoDetectChangesEnabled = true;
            }
            
            _logger.LogInformation("BatchSync завершён для пользователя {UserId}: {Synced} успешно, {Failed} с ошибками", userId, response.Synced, response.Failed);

            return response;
        }
        private async Task<int> SyncSingleInternalAsync(
        int userId,
        WorkoutDto dto,
        List<Device> devicesCache,
        HashSet<string> existingIdentifiers,
        CancellationToken ct)
        {
            var device = devicesCache.FirstOrDefault(d => d.SerialNumber == dto.SerialNumberDevice);

            if (device == null)
            {
                device = new Device
                {
                    SerialNumber = dto.SerialNumberDevice,
                    UserId = userId,
                    IsVirtual = dto.SerialNumberDevice == null,
                    CreatedAt = DateTime.UtcNow,
                };

                _context.Devices.Add(device);
                devicesCache.Add(device);

                await _context.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "Создано новое {Type} устройство {SerialNumber} для пользователя {UserId}",
                    device.IsVirtual ? "виртуальное" : "реальное",
                    dto.SerialNumberDevice?.ToString() ?? "<virtual>",
                    userId);
            }

            // ✅ 2. Проверка дубликата через HashSet (быстро, O(1))
            if (existingIdentifiers.Contains(dto.Identifier))
            {
                // Загружаем только если реально есть
                var existingWorkout = await _context.Workouts
                    .Include(w => w.WorkoutSamples)
                    .Include(w => w.Laps)
                    .FirstOrDefaultAsync(x => x.Identifier == dto.Identifier, ct);

                if (existingWorkout != null)
                {
                    if (existingWorkout.WorkoutSamples != null)
                        _context.WorkoutSamples.RemoveRange(existingWorkout.WorkoutSamples);
                    if (existingWorkout.Laps != null)
                        _context.Laps.RemoveRange(existingWorkout.Laps);
                    _context.Workouts.Remove(existingWorkout);
                }

                existingIdentifiers.Remove(dto.Identifier); // Чтобы не блокировать повторный добав
            }

            Workout workout = dto.SportType.ToLower() switch
            {
                "running" => new WorkoutRunning { AvgCadence = dto.WorkoutRunningAvgCadence },
                "cycling" => new WorkoutCycling { AvgCadence = dto.AvgCadence, AvgPower = dto.AvgPower },
                "skiing" => new WorkoutSkiing
                {
                    StyleOfSkiing = dto.StyleOfSkiing switch
                    {
                        1 => WorkoutSkiing.Styles.Skating,
                        2 => WorkoutSkiing.Styles.DoublePoling,
                        _ => WorkoutSkiing.Styles.Classic
                    }
                },
                _ => new WorkoutOther()
            };

            workout.DeviceId = device.DeviceId;
            workout.AvgHeartRate = dto.AvgHeartRate;
            workout.AvgSpeedMetersPerHour = dto.AvgSpeedMetersPerHour;
            workout.Description = dto.Description;
            workout.DistanceMeters = dto.DistanceMeters;
            workout.ElevationGain = dto.ElevationGain;
            workout.SerialNumberDevice = dto.SerialNumberDevice ?? 0;
            workout.TimeStart = dto.TimeStart;
            workout.TimeEnd = dto.TimeEnd;
            workout.SetIdentifier();

            // Samples
            if ((dto.WorkoutSamples?.Count ?? 0) != 0)
            {
                workout.WorkoutSamples = dto.WorkoutSamples!.Select(x =>
                {
                    WorkoutSamples sample = dto.SportType.ToLower() switch
                    {
                        "running" => new WorkoutSamplesRunning { Cadence = x.WorkoutSamplesRunningCadence },
                        "cycling" => new WorkoutSamplesCycling { Power = x.Power, Cadence = x.Cadence },
                        _ => new WorkoutSamplesOther()
                    };

                    sample.Altitude = x.Altitude;
                    sample.DistanceMeters = x.DistanceMeters;
                    sample.HeartRate = x.HeartRate;
                    sample.Latitude = x.Latitude;
                    sample.Longitude = x.Longitude;
                    sample.SpeedMetersPerHour = x.SpeedMetersPerHour;
                    sample.TimeSeconds = x.TimeSeconds;
                    sample.Workout = workout;

                    return sample;
                }).ToList();
            }

            // Laps
            if ((dto.Laps?.Count ?? 0) != 0)
            {
                workout.Laps = dto.Laps!.Select(x =>
                {
                    Lap lap = dto.SportType.ToLower() switch
                    {
                        "running" => new LapRunning { AvgCadence = x.LapRunningAvgCadence },
                        "cycling" => new LapCycling { AvgPower = x.AvgPower, AvgCadence = x.AvgCadence },
                        _ => new LapOther()
                    };

                    lap.AvgHeartRate = x.AvgHeartRate;
                    lap.AvgSpeedMetersPerHour = x.AvrSpeedMetersPerHour;
                    lap.DistanceMeters = x.DistanceMeters;
                    lap.ElevationGain = x.ElevationGain;
                    lap.TimeEnd = x.TimeEnd;
                    lap.Workout = workout;

                    return lap;
                }).ToList();
            }

            _context.Workouts.Add(workout);

            _logger.LogInformation(
                "Тренировка {Identifier} ({Type}) устройства {DeviceId} пользователя {UserId} подготовлена",
                workout.Identifier, dto.SportType, device.DeviceId, userId);

            return workout.WorkoutId;
        }
        public async Task<bool> IsWorkoutExistAsync(string identifier, CancellationToken ct = default)
        {
            Console.WriteLine(_context.Workouts.Count());
            return await _context.Workouts.AnyAsync(x => x.Identifier == identifier, ct);
        }
    }
}
