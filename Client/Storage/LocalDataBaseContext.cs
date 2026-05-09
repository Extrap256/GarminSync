using Microsoft.EntityFrameworkCore;
using Client.Storage.Models;

namespace Client.Storage
{
    public class LocalDataBaseContext : DbContext
    {
        //public DbSet<User> Users { get; set; } = null!;
        //public DbSet<Device> Devices { get; set; } = null!;
        public DbSet<Workout> Workouts { get; set; } = null!;
        public DbSet<WorkoutRunning> WorkoutRunnings { get; set; } = null!;
        public DbSet<WorkoutCycling> WorkoutCyclings { get; set; } = null!;
        public DbSet<WorkoutSkiing> WorkoutSkiings { get; set; } = null!;
        public DbSet<WorkoutSamples> WorkoutSamples { get; set; } = null!;
        public DbSet<WorkoutSamplesRunning> WorkoutSamplesRunnings { get; set; } = null!;
        public DbSet<WorkoutSamplesCycling> WorkoutSamplesCyclings { get; set; } = null!;
        public DbSet<WorkoutSamplesOther> WorkoutSamplesOthers { get; set; } = null!;
        public DbSet<Lap> Laps { get; set; } = null!;
        public DbSet<LapCycling> LapCyclings { get; set; } = null!;
        public DbSet<LapRunning> LapRunnings { get; set; } = null!;
        public DbSet<LapOther> LapSkiings { get; set; } = null!;

        public LocalDataBaseContext(DbContextOptions<LocalDataBaseContext> options)
            : base(options)
        {
            Database.EnsureCreated();
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}