using System.Security.Claims;
using Server.API.Models.Report;
using Server.Common.Exceptions;
using Server.Core.Interfaces;

namespace Server.API.Endpoints
{
    public static class ReportEndpoints
    {
        public static void MapReportEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/reports").RequireAuthorization();

            //Получение персонального отчета
            group.MapPost("/personal", async (
                HttpContext context,
                IReportService reportService,
                ReportRequestDto dto,
                CancellationToken ct) =>
            {
                int userId = GetUserId(context);
                var report = await reportService.GetUserReportAsync(userId, dto.From, dto.To, ct);
                return Results.Ok(new { content = report });
            });

            //Получение отчета по группе
            group.MapPost("/group/{groupId:int}", async (
                int groupId,
                ReportRequestDto dto,
                HttpContext context,
                IReportService reportService,
                CancellationToken ct) =>
            {
                // Добавить проверку на тренера
                var report = await reportService.GetGroupReportAsync(groupId, dto.From, dto.To, ct);
                return Results.Ok(new { content = report });
            });
        }

        private static int GetUserId(HttpContext context)
        {
            var claim = context.User.FindFirst(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedException("Отсутствует UserID");
            return int.Parse(claim.Value);
        }
    }
}
