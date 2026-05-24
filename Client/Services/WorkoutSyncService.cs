using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using Client.Core.Interfaces;
using Client.Core.Interfaces.DTO.API;
using Client.Infrastructure.API;
using Client.Infrastructure.Storage;
using EntityClassLibraryClient.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Client.Services
{
    public class WorkoutSyncService (
        IHttpClientFactory httpClientFactory,
        IOptions<ApiSettings> options,
        LocalDataBaseContext dbContext) : IWorkoutSyncService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly ApiSettings _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
        private readonly LocalDataBaseContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        private readonly HttpClient _httpClient = httpClientFactory?.CreateClient("GarminApi") ?? throw new ArgumentNullException(nameof(httpClientFactory));

        public async Task<SyncWorkoutResponse> SyncWorkoutAsync(SyncWorkoutRequest request, CancellationToken ct = default)
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"{_settings.BaseUrl}/workouts/sync",
                request,
                JsonOptions,
                ct);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException("Токен истек");

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                throw new InvalidOperationException("Тренировка уже есть на сервере");

            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                throw new ArgumentException($"Ошибка валидации: {error}");
            }

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<SyncWorkoutResponse>(JsonOptions, ct);

            return result ?? throw new InvalidOperationException("Пустой ответ");
        }

        public async Task<int> SyncAllUnsyncedAsync(CancellationToken ct = default)
        {
            var unsyncedWorkouts = await _dbContext.Workouts
                .Include(w => w.Device)
                .Include(w => w.Laps)
                .Include(w => w.WorkoutSamples)
                .Where(w => !w.IsSynced)
                .ToListAsync(ct);

            if (unsyncedWorkouts.Count == 0)
                return 0;

            int syncedCount = 0;

            foreach (var workout in unsyncedWorkouts)
            {
                try
                {
                    var request = MapToRequest(workout);
                    await SyncWorkoutAsync(request, ct);

                    workout.IsSynced = true;
                    workout.LastSyncedAt = DateTime.UtcNow;
                    syncedCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка синхронизации {workout.Identifier}: {ex.Message}");

                }
            }

            await _dbContext.SaveChangesAsync(ct);

            return syncedCount;
        }

        private SyncWorkoutRequest MapToRequest(Workout workout)
        {
            SyncWorkoutRequest request = new()
            {
                TimeStart = workout.TimeStart,
                TimeEnd = workout.TimeEnd,
                Identifier = workout.Identifier,
                SportType = GetSportType(workout),
                SerialNumberDevice = workout.SerialNumberDevice,
                DistanceMeters = workout.DistanceMeters,
                Duration = (int)(workout.TimeEnd - workout.TimeStart).TotalSeconds,
                AvgHeartRate = workout.AvgHeartRate,
                AvgSpeedMetersPerHour = workout.AvgSpeedMetersPerHour,
                ElevationGain = workout.ElevationGain,
                Description = workout.Description
            };

            if ((workout.WorkoutSamples?.Count ?? 0) != 0)
            {
                request.WorkoutSamples = workout.WorkoutSamples.Select(s => new SyncWorkoutSampleRequest()
                {
                    TimeSeconds = s.TimeSeconds,
                    DistanceMeters = s.DistanceMeters,
                    SpeedMetersPerHour = s.SpeedMetersPerHour,
                    HeartRate = s.HeartRate,
                    Altitude = s.Altitude,
                    Latitude = s.Latitude,
                    Longitude = s.Longitude,
                    Discriminator = s.GetType().Name
                }).ToList();
            }

            if ((workout.Laps?.Count ?? 0) != 0)
            {
                request.Laps = workout.Laps!.Select(l => new SyncLapRequest
                {
                    TimeEnd = l.TimeEnd,
                    DistanceMeters = l.DistanceMeters,
                    AvrSpeedMetersPerHour = l.AvgSpeedMetersPerHour,
                    AvgHeartRate = l.AvgHeartRate,
                    ElevationGain = l.ElevationGain,
                    Discriminator = l.GetType().Name
                }).ToList();
            }

            return request;
        }

        private static string GetSportType(Workout workout)
        {
            return workout switch
            {
                WorkoutRunning => "running",
                WorkoutCycling => "cycling",
                WorkoutSkiing => "skiing",
                _ => "other"
            };
        }

    }
}
