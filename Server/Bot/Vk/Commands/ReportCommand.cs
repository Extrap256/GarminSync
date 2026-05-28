using Server.Core.Interfaces;
using Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Server.Bot.Vk.Commands
{
    public class ReportCommand : IBotCommand
    {
        private readonly IVkMessageSender _messageSender;
        private readonly ApplicationDbContext _context;
        private readonly IReportService _reportService;

        public ReportCommand(
            IVkMessageSender messageSender,
            ApplicationDbContext context,
            IReportService reportService)
        {
            _messageSender = messageSender;
            _context = context;
            _reportService = reportService;
        }

        public bool CanHandle(string text) =>
            !string.IsNullOrWhiteSpace(text) &&
            text.Trim().StartsWith("/report", StringComparison.OrdinalIgnoreCase);

        public async Task ExecuteAsync(long vkUserId, string text)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.VkId == vkUserId);
            if (user == null)
            {
                await _messageSender.SendMessageAsync(vkUserId,
                    "Сначала войди: /login");
                return;
            }

            var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            DateTime from, to;

            if (parts.Length >= 3 &&
                DateTime.TryParse(parts[1], out from) &&
                DateTime.TryParse(parts[2], out to))
            {
                // ОК
            }
            else
            {
                to = DateTime.UtcNow;
                from = to.AddMonths(-1);
            }

            var report = await _reportService.GetUserReportAsync(user.UserId, from, to);
            await _messageSender.SendMessageAsync(vkUserId, report);
        }
    }
}