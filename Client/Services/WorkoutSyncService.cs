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
        private const int BatchSizeMax = 50;

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
            var content = ApiHelper.CreateJsonContent(request);
            var response = await _httpClient.PostAsync($"{_settings.BaseUrl}/workouts/sync", content, ct);

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

        public async Task<BatchSyncResult> SyncAllUnsyncedBatchAsync(CancellationToken ct = default)
        {
            var unsyncedWorkouts = await _dbContext.Workouts
                .Include(w => w.Device)
                .Include(w => w.Laps)
                .Include(w => w.WorkoutSamples)
                .Where(w => !w.IsSynced)
                .ToListAsync(ct);

            if (unsyncedWorkouts.Count == 0)
                return new BatchSyncResult(0, 0, 0, new List<BatchSyncError>());

            var totalSynced = 0;
            var totalFailed = 0;
            var allErrors = new List<BatchSyncError>();
            var syncedEntities = new List<Workout>();

            var chunks = unsyncedWorkouts
                .Select((w, i) => new { Workout = w, Index = i })
                .GroupBy(x => x.Index / BatchSizeMax)
                .Select(g => g.Select(x => x.Workout).ToList())
                .ToList();

            foreach (var chunk in chunks)
            {
                ct.ThrowIfCancellationRequested();

                var request = new BatchSyncWorkoutsRequest()
                {
                    Workouts = chunk.Select(x => MapToRequest(x)).ToList()
                };

                BatchSyncWorkoutsResponse? response = null;
                try
                {
                    response = await SendBatchAsync(request, ct);
                }
                catch (HttpRequestException ex)
                {
                    allErrors.Add(new BatchSyncError()
                    {
                        Identifier = "<network>",
                        Error = $"Сетевая ошибка: {ex.Message}"
                    });
                }
                catch (UnauthorizedAccessException)
                {
                    throw;
                }

                if (response == null)
                    continue;

                var failedIdentifiers = new HashSet<string>(response.Errors.Select(e => e.Identifier));

                foreach (var workout in chunk)
                {
                    if (failedIdentifiers.Contains(workout.Identifier))
                        totalFailed++;
                    else
                    {
                        workout.IsSynced = true;
                        workout.LastSyncedAt = DateTime.UtcNow;
                        syncedEntities.Add(workout);
                        totalSynced++;
                    }
                }

                allErrors.AddRange(response.Errors);
            }

            if (syncedEntities.Count != 0)
                await _dbContext.SaveChangesAsync(ct);

            return new BatchSyncResult(
                TotalProcessed: totalSynced + totalFailed,
                TotalSynced: totalSynced,
                TotalFailed: totalFailed,
                Errors: allErrors);
        }

        private async Task<BatchSyncWorkoutsResponse?> SendBatchAsync(BatchSyncWorkoutsRequest request, CancellationToken ct)
        {
            var content = ApiHelper.CreateJsonContent(request);
            var response = await _httpClient.PostAsync($"{_settings.BaseUrl}/workouts/sync/batch", content, ct);

            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException("Токен истек. Войдите заново");

            if (response.StatusCode is System.Net.HttpStatusCode.OK 
                or System.Net.HttpStatusCode.MultiStatus
                or System.Net.HttpStatusCode.BadRequest)
            {
                return await response.Content.ReadFromJsonAsync<BatchSyncWorkoutsResponse>(JsonOptions, ct);
            }

            response.EnsureSuccessStatusCode();
            return null;
        }

        private static SyncWorkoutRequest MapToRequest(Workout workout)
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
                request.WorkoutSamples = workout.WorkoutSamples!.Select(s => new SyncWorkoutSampleRequest()
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
