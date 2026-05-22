using EntityClassLibraryServer.Models;
using Microsoft.EntityFrameworkCore;
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
            Device? device = null;

            //Если виртуальное устройство
            dto.SerialNumberDevice ??= 0;

            device = await _context.Devices
                                .FirstOrDefaultAsync(x =>
                                x.UserId == userId &&
                                x.SerialNumber == dto.SerialNumberDevice,
                                ct);

            if (device == null)
            {
                device = new()
                {
                    SerialNumber = dto.SerialNumberDevice!,
                    UserId = userId,
                    User = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId, ct),
                    CreatedAt = DateTime.UtcNow,
                };

                _context.Devices.Add(device);
                await _context.SaveChangesAsync(ct);
                _logger.LogInformation("Создано новое устройство {SerialNumber} для пользователя {UserId}", dto.SerialNumberDevice, userId);
            }

            bool isDuplicate = await IsWorkoutExistAsync(dto.Identifier, ct);
            if (isDuplicate)
                throw new DuplicateWorkoutException("Тренировка уже есть в БД");

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
            workout.SerialNumberDevice = dto.SerialNumberDevice.Value;
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
            _context.Entry(resultW.Device).Reference(d => d.User).Load();

            _logger.LogInformation("Тренировка {WorkoutId} устройства {DeviceId} пользователя {UserId} успешно сохранена", resultW.WorkoutId, resultW.Device?.DeviceId, resultW.Device?.User?.UserId);

            return resultW.WorkoutId;
        }

        public async Task<bool> IsWorkoutExistAsync(string identifier, CancellationToken ct = default)
        {
            Console.WriteLine(_context.Workouts.Count());
            return await _context.Workouts.AnyAsync(x => x.Identifier == identifier, ct);
        }
    }
}
