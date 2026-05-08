using Client.Core.Interfaces;
using Client.Storage.Models;
using Microsoft.EntityFrameworkCore;

namespace Client.Storage
{
    public class ActivityRepository : IActivityRepository
    {
        private LocalDataBaseContext _dbContext;

        public ActivityRepository(LocalDataBaseContext dbContext)
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

        public Task AddAsync(Workout workout, CancellationToken ct = default)
        {
            if (workout == null)
                throw new ArgumentNullException(nameof(workout));

            //if (string.IsNullOrWhiteSpace(workout.Identifier))
            //{
            //    //
            //}

            _dbContext.Workouts.Add(workout);

            return Task.CompletedTask;
        }

        public async Task SaveChangeAsync(CancellationToken ct = default)
        {
            await _dbContext.SaveChangesAsync(ct);
        }
    }
}
