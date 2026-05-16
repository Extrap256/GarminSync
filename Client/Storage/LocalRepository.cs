using Client.Core.Interfaces;
using Client.Core.Interfaces.DTO;
using EntityClassLibrary.Models;
using Microsoft.EntityFrameworkCore;

namespace Client.Storage
{
    public class LocalRepository : ILocalRepository
    {
        private LocalDataBaseContext _dbContext;

        public LocalRepository(LocalDataBaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> ExistByIdentifierAsync(string identifier, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return false;

            return await _dbContext.Workouts
                .AsNoTracking()
                .AnyAsync(workout => workout.Identifier == identifier, ct);
        }

        public Task AddAsync(Workout workout, uint serialNumber, CancellationToken ct = default)
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
    }
}
