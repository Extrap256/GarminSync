using Microsoft.EntityFrameworkCore;
using EntityClassLibraryClient.Models;

namespace Client.Infrastructure.Storage
{
    public class LocalDataBaseContext : DbContext
    {
        //public DbSet<User> Users { get; set; } = null!;
        public DbSet<Device> Devices { get; set; } = null!;
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

            // Настройка связи Workout => Device
            modelBuilder.Entity<Workout>()
                .HasOne(w => w.Device)
                .WithMany(d => d.Workouts)
                .HasForeignKey(w => w.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);

            
            modelBuilder.Entity<Workout>()
                .HasDiscriminator<string>("WorkoutType")
                //.HasValue<Workout>("BaseWorkout")
                .HasValue<WorkoutRunning>("WorkoutRunning")
                .HasValue<WorkoutCycling>("WorkoutCycling")
                .HasValue<WorkoutSkiing>("WorkoutSkiing")
                .HasValue<WorkoutOther>("WorkoutOther");

            // То же самое для лапов
            modelBuilder.Entity<Lap>()
                .HasDiscriminator<string>("LapType")
                //.HasValue<Lap>("BaseLap")
                .HasValue<LapRunning>("LapRunning")
                .HasValue<LapCycling>("LapCycling")
                .HasValue<LapOther>("LapOther");

            // То же самое для семплов
            modelBuilder.Entity<WorkoutSamples>()
                .HasDiscriminator<string>("SampleType")
                //.HasValue<WorkoutSamples>("BaseSample")
                .HasValue<WorkoutSamplesRunning>("WorkoutSamplesRunning")
                .HasValue<WorkoutSamplesCycling>("WorkoutSamplesCicling") // Обрати внимание на опечатку в твоём названии
                .HasValue<WorkoutSamplesOther>("WorkoutSamplesOther");
        }
    }
}