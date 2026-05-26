using Client.Core.Interfaces;
using Client.Core.Interfaces.DTO;
using EntityClassLibraryClient.Models;
using Microsoft.EntityFrameworkCore;

namespace Client.Infrastructure.Storage
{
    public class LocalRepository(LocalDataBaseContext dbContext) : ILocalRepository
    {
        private readonly LocalDataBaseContext _dbContext = dbContext;

        public async Task<bool> ExistByIdentifierAsync(string identifier, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return false;

            return await _dbContext.Workouts
                .AsNoTracking()
                .AnyAsync(workout => workout.Identifier == identifier, ct);
        }

        public Task AddAsync(Workout workout, uint? serialNumber, CancellationToken ct = default)
        {
            if (workout == null)
                throw new ArgumentNullException(nameof(workout));

            if (workout.Device == null)
                throw new ArgumentNullException(nameof(workout.Device));


            _dbContext.Devices.Include(d => d.Workouts)
                .First(d => d.SerialNumber == workout.Device.SerialNumber).Workouts
                .Add(workout);

            return Task.CompletedTask;
        }

        public async Task SaveChangeAsync(CancellationToken ct = default)
        {
            await _dbContext.SaveChangesAsync(ct);
        }

        public async Task<Device> GetOrCreateDeviceAsync(uint serialNumber)
        {
            if (!_dbContext.Devices.Any(x => x.SerialNumber == serialNumber))
                await CreateDeviceAsync(new DeviceData { SerialNumber = serialNumber });

            return _dbContext.Devices.First(x => x.SerialNumber == serialNumber);
        }

        public async Task CreateDeviceAsync(DeviceData deviceData, string? description = null, string? name = null, bool isVirtual = false)
        {
            Device device = new()
            {
                SerialNumber = deviceData.SerialNumber,
                IsVirtual = isVirtual,
                Model = deviceData.Product,
                Name = name,
                Description = description
            };

            
            _dbContext.Devices.Add(device);

            await SaveChangeAsync();
        }

        public async Task<IReadOnlyList<Workout>> GetAllWorkoutsAsync(CancellationToken ct = default)
        {
            return await _dbContext.Workouts
                .Include(w => w.Device)
                .OrderByDescending(w => w.TimeStart)
                .AsNoTracking()
                .ToListAsync(cancellationToken: ct);
        }
        public async Task<Workout?> GetWorkoutByIdAsync(int workoutId, CancellationToken ct = default)
        {
            return await _dbContext.Workouts
                .Include(w => w.Device)
                .Include(w => w.Laps)
                .Include(w => w.WorkoutSamples)
                .FirstOrDefaultAsync(ct);
        }

        // Обновление уже сохраненной тренировки
        public async Task UpdateWorkoutAsync(Workout workout, CancellationToken ct = default)
        {
            if (workout == null)
                throw new ArgumentNullException(nameof(workout));

            workout.SetIdentifier();

            workout.MarkAsUnsynced();

            bool conflict = await _dbContext.Workouts
                .AnyAsync(w => w.WorkoutId != workout.WorkoutId && w.Identifier == workout.Identifier, ct);
            if (conflict)
                throw new InvalidOperationException($"Тренировка с идентификатором '{workout.Identifier}' уже сущестует");

            _dbContext.Workouts.Update(workout);
        }

        public async Task<Device> GetOrCreateVirtualDeviceAsync(CancellationToken ct = default)
        {
            var virtualDevice = await _dbContext.Devices.FirstOrDefaultAsync(x => x.IsVirtual, ct);

            if (virtualDevice != null)
                return virtualDevice;

            virtualDevice = new()
            {
                Name = "Virtual Device",
                SerialNumber = null,
                IsVirtual = true,
                Description = "Для ручных тренировок",
                Workouts = new()
            };

            _dbContext.Devices.Add(virtualDevice);
            await _dbContext.SaveChangesAsync(ct);

            return virtualDevice;
        }
    }
}
