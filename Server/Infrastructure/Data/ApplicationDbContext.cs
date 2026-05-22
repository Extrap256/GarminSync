using EntityClassLibrary.Models;
using Microsoft.EntityFrameworkCore;


namespace Server.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Device> Devices { get; set; } = null!;

        public DbSet<Group> Groups { get; set; } = null!;
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

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
            Database.EnsureCreated();
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Участники: Group.Members <=> User.Groups
            modelBuilder.Entity<Group>()
                .HasMany(g => g.Members)
                .WithMany(u => u.Groups)
                .UsingEntity(
                "GroupMembers",
                j => j.HasOne(typeof(User)).WithMany().OnDelete(DeleteBehavior.Cascade),
                j => j.HasOne(typeof(Group)).WithMany().OnDelete(DeleteBehavior.Cascade),
                j => j.ToTable("GroupMembers"));

            // Тренеры: Group.Trainers <=> User.TrainedGroups
            modelBuilder.Entity<Group>()
                .HasMany(g => g.Trainers)
                .WithMany(u => u.TrainedGroups)
                .UsingEntity(
                    "GroupTrainers",
                    j => j.HasOne(typeof(User)).WithMany().OnDelete(DeleteBehavior.Cascade),
                    j => j.HasOne(typeof(Group)).WithMany().OnDelete(DeleteBehavior.Cascade),
                    j => j.ToTable("GroupTrainers"));

            // Создатель: Group.Creator => User.CreatedGroups
            modelBuilder.Entity<Group>()
                .HasOne(g => g.Creator)
                .WithMany(u => u.CreatedGroups)
                .HasForeignKey(g => g.CreatorId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
