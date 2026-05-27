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
            Device? device = await _context.Devices
                                .FirstOrDefaultAsync(x =>
                                x.UserId == userId &&
                                x.SerialNumber == dto.SerialNumberDevice,
                                ct);

            if (device == null)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId, ct) ?? throw new NotFoundException("Пользователь не найден");

                device = new()
                {
                    SerialNumber = dto.SerialNumberDevice,
                    UserId = userId,
                    User = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId, ct),
                    IsVirtual = dto.SerialNumberDevice == null,
                    CreatedAt = DateTime.UtcNow,
                };

                _context.Devices.Add(device);
                await _context.SaveChangesAsync(ct);
                _logger.LogInformation("Создано новое {Type} устройство {SerialNumber} для пользователя {UserId}",
                    device.IsVirtual ? "виртуальное" : "реальное",
                    dto.SerialNumberDevice?.ToString() ?? "<virtual>",
                    userId);
            }

            Workout? existingWorkout = await _context.Workouts
                .Include(w => w.WorkoutSamples)
                .Include(w => w.Laps)
                .FirstOrDefaultAsync(x => x.Identifier == dto.Identifier, ct);


            if (existingWorkout != null)
            {
                _context.WorkoutSamples.RemoveRange(existingWorkout.WorkoutSamples ?? []);
                _context.Laps.RemoveRange(existingWorkout.Laps ?? []);
                _context.Workouts.Remove(existingWorkout);
            }

            Workout workout = dto.SportType switch
            {
                "running" => new WorkoutRunning() { AvgCadence = dto.WorkoutRunningAvgCadence },
                "cycling" => new WorkoutCycling() { AvgCadence = dto.AvgCadence, AvgPower = dto.AvgPower },
                "skiing" => new WorkoutSkiing() { StyleOfSkiing = dto.StyleOfSkiing switch
                {
                    //Стоит подкорректировать
                    1 => WorkoutSkiing.Styles.Skating,
                    2 => WorkoutSkiing.Styles.DoublePoling,
                    _ => WorkoutSkiing.Styles.Classic
                } },
                _ => new WorkoutOther()
            };
            workout.DeviceId = device.DeviceId;
            workout.Device = device;
            workout.AvgHeartRate = dto.AvgHeartRate;
            workout.AvgSpeedMetersPerHour = dto.AvgSpeedMetersPerHour;
            workout.Description = dto.Description;
            workout.DistanceMeters = dto.DistanceMeters;
            workout.ElevationGain = dto.ElevationGain;
            workout.SerialNumberDevice = dto.SerialNumberDevice ?? 0;
            workout.TimeStart = dto.TimeStart;
            workout.TimeEnd = dto.TimeEnd;
            workout.SetIdentifier();

            workout.WorkoutSamples = dto.WorkoutSamples?.Select(x =>
            {
                WorkoutSamples sample = dto.SportType.ToLower() switch
                {
                    "running" => new WorkoutSamplesRunning() { Cadence = x.WorkoutSamplesRunningCadence },
                    "cycling" => new WorkoutSamplesCycling() { Power = x.Power, Cadence = x.Cadence },
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


            workout.Laps = dto.Laps?.Select(x =>
            {
                Lap lap = dto.SportType.ToLower() switch
                {
                    "running" => new LapRunning() { AvgCadence = x.LapRunningAvgCadence },
                    "cycling" => new LapCycling() { AvgPower = x.AvgPower, AvgCadence = x.AvgCadence },
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


            _context.Workouts.Add(workout);
            await _context.SaveChangesAsync(ct);

            Workout? resultW = await _context.Workouts.FirstOrDefaultAsync(x => x.Identifier == dto.Identifier, ct) ?? workout;
            _context.Entry(resultW).Reference(w => w.Device).Load();
            _context.Entry(resultW.Device!).Reference(d => d.User).Load();

            _logger.LogInformation("Тренировка {WorkoutId} устройства {DeviceId} пользователя {UserId} успешно сохранена", resultW.WorkoutId, resultW.Device?.DeviceId, resultW.Device?.User?.UserId);

            return resultW.WorkoutId;
        }
        public async Task<WorkoutBatchSyncResponse> SyncWorkoutsBatchAsync(int userId, List<WorkoutDto> dtos, CancellationToken ct = default)
        {
            var response = new WorkoutBatchSyncResponse();

            foreach (var dto in dtos)
            {
                try
                {
                    var strategy = _context.Database.CreateExecutionStrategy();

                    await strategy.ExecuteAsync(async () =>
                    {
                        await using IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync(ct);

                        try
                        {
                            var workoutId = await SyncWorkoutAsync(userId, dto, ct);
                            await transaction.CommitAsync(ct);

                            response.SyncedIds.Add(workoutId);
                            response.Synced++;
                        }
                        catch
                        {
                            await transaction.RollbackAsync(ct);
                            throw;
                        }
                    });
                }
                catch (Exception ex)
                {
                    //Очищение ChangeTracker
                    _context.ChangeTracker.Clear();

                    response.Errors.Add(new BatchSyncError()
                    {
                        Identifer = dto.Identifier ?? "<null>",
                        Error = ex.Message
                    });
                    response.Failed++;

                    _logger.LogWarning(ex, "Ошибка синхронизации тренировки {Identifier} пользователя {UserId}", dto.Identifier, userId);
                }
            }

            _logger.LogInformation("BatchSync завершен для пользовтеля {UserId}: {Synced} успешно, {Failed} с ошибками", userId, response.Synced, response.Failed);

            return response;
        }
        public async Task<bool> IsWorkoutExistAsync(string identifier, CancellationToken ct = default)
        {
            Console.WriteLine(_context.Workouts.Count());
            return await _context.Workouts.AnyAsync(x => x.Identifier == identifier, ct);
        }
    }
}
