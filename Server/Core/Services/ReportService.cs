using System.Text;
using EntityClassLibraryServer.Models;
using Microsoft.EntityFrameworkCore;
using Server.Common.Exceptions;
using Server.Core.Interfaces;
using Server.Infrastructure.Data;

namespace Server.Core.Services
{
    public class ReportService (ApplicationDbContext context) : IReportService
    {
        private readonly ApplicationDbContext _context = context;

        public async Task<string> GetUserReportAsync(int userId, DateTime from, DateTime to, CancellationToken ct)
        {
            from = EnsureUtc(from);
            to = EnsureUtc(to);
            var workouts = await _context.Workouts
                .Include(w => w.Device)
                .ThenInclude(d => d!.User)
                .Where(w => w.Device!.UserId == userId && w.TimeStart >= from && w.TimeEnd <= to)
                .ToListAsync(ct);

            if (workouts.Count == 0)
                return "За выбранный период тренировок не найдено";

            double totalDisnanceKiloMeters = workouts.Sum(w => (w.DistanceMeters ?? 0) / 1000.0);
            int totalCount = workouts.Count;
            TimeSpan totalTime = workouts.Aggregate(TimeSpan.Zero, (sum, w) => sum + (w.TimeEnd - w.TimeStart));

            StringBuilder sb = new();
            sb.AppendLine($"Отчет за период: {from:dd.MM.yyyy} - {to:dd.MM.yyyy}");
            sb.AppendLine($"Всего тренировок: {totalCount}");
            sb.AppendLine($"Общая дистанция: {totalDisnanceKiloMeters} км");
            sb.AppendLine($"Общее время: {totalTime.Days}д {totalTime.Hours}ч {totalTime.Minutes}м");


            return sb.ToString();
        }

        private static DateTime EnsureUtc(DateTime dt)
        {
            return dt.Kind switch
            {
                DateTimeKind.Utc => dt,
                DateTimeKind.Local => dt.ToUniversalTime(),
                _ => DateTime.SpecifyKind(dt, DateTimeKind.Utc) // Unspecified → считаем UTC
            };
        }

        public async Task<string> GetGroupReportAsync(int groupId, DateTime from, DateTime to, CancellationToken ct = default)
        {
            from = EnsureUtc(from);
            to = EnsureUtc(to);
            Group group = _context.Groups
                .Include(g => g.Members)
                .FirstOrDefault(g => g.GroupId == groupId) ?? throw new NotFoundException("Группа не найдена");

            var members = group.Members;

            if (members.Count == 0)
                return "В группе нет участников";

            var sb = new StringBuilder();
            sb.AppendLine($"Отчет группы \"{group.Name}\" за {from:dd.MM.yyyy} - {to:dd.MM.yyyy}");
            sb.AppendLine(new string('-', 40));

            foreach (User member in members)
            {
                var memberWorkouts = await _context.Workouts
                    .Include(w => w.Device)
                    .ThenInclude(d => d!.User)
                    .Where(w => w.Device!.UserId == member.UserId && w.TimeStart >= from && w.TimeStart <= to).ToListAsync(ct);

                double distanceKilometers = memberWorkouts.Sum(w => w.DistanceMeters ?? 0) / 1000.0;
                double totalTimeHours = memberWorkouts.Aggregate(TimeSpan.Zero, (sum, w) => sum + (w.TimeEnd - w.TimeStart)).TotalHours;
                int count = memberWorkouts.Count;

                string name = $"{member.FirstName} {member.LastName}".Trim();
                if (string.IsNullOrEmpty(name))
                    name = member.Login;

                string yob = member.YearOfBirth?.ToString() ?? "";

                sb.AppendLine($"{name} {yob}: {distanceKilometers:F2} км {totalTimeHours:F2} ч ({count} тр.)");
            }

            return sb.ToString();
        }
    }
}
