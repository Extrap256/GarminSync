using Microsoft.EntityFrameworkCore;
using Client.Storage.Models;

namespace Client.Storage
{
    public class LocalDataBaseContext : DbContext
    {
        private string _connectionString;

        public DbSet<User> Users { get; set; } = null!;

        public DbSet<Device> Devices { get; set; } = null!;

        public DbSet<Workout> Workouts { get; set; } = null!;
        public DbSet<WorkoutRunning> WorkoutRunnings { get; set; } = null!;
        public DbSet<WorkoutCycling> WorkoutCyclings { get; set; } = null!;
        public DbSet<WorkoutSkiing> WorkoutSkiings { get; set; } = null!;

        public DbSet<WorkoutSamples> WorkoutSamples { get; set; } = null!;
        public DbSet<WorkoutSamplesRunning> WorkoutSamplesRunnings { get; set; } = null!;
        public DbSet<WorkoutSamplesCicling> WorkoutSamplesCiclings { get; set; } = null!;
        public DbSet<WorkoutSamplesOther> WorkoutSamplesOthers { get; set; } = null!;

        public DbSet<Lap> Laps { get; set; } = null!;
        public DbSet<LapCycling> LapCyclings { get; set; } = null!;
        public DbSet<LapRunning> LapRunnings { get; set; } = null!;
        public DbSet<LapSkiing> LapSkiings { get; set; } = null!;
        
        
        public LocalDataBaseContext(string connectionString)
        {
            this._connectionString = connectionString;

            Database.EnsureCreated();

            this.Users.Add(new User()
            {
                FirstName = "Salavat",
                LastName = "Kutlov",
                YearOfBirth = 2004,
                Hash = "12345"
            });
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(_connectionString);
        }
    }
}
