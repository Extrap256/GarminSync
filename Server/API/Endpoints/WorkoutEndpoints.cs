using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Server.API.Models.Workout;
using Server.Common.Exceptions;
using Server.Core.Interfaces;

namespace Server.API.Endpoints
{
    public static class WorkoutEndpoints
    {
        public static void MapWorkoutEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/workouts").RequireAuthorization();

            group.MapPost("/sync/batch", async (
                WorkoutBatchSyncRequest request,
                IWorkoutSyncService syncService,
                HttpContext context,
                CancellationToken ct) =>
            {
                var validationContext = new ValidationContext(request);
                var validationResults = new List<ValidationResult>();
                if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
                {
                    return Results.ValidationProblem(validationResults.ToDictionary(
                        r => r.MemberNames.FirstOrDefault() ?? "request",
                        r => new[] { r.ErrorMessage! }));
                }

                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedException("Токен не соедржит UserId");

                if (!int.TryParse(userIdClaim.Value, out int userId))
                    throw new UnauthorizedException("неверный формат UserId в токене");

                var result = await syncService.SyncWorkoutsBatchAsync(userId, request.Workouts, ct);

                if (result.Failed > 0 && result.Synced > 0)
                    return Results.Json(result, statusCode: StatusCodes.Status207MultiStatus);

                if (result.Synced == 0)
                    return Results.BadRequest(result);

                return Results.Ok(result);
            })
                .Produces<WorkoutBatchSyncResponse>(StatusCodes.Status200OK)
                .Produces<WorkoutBatchSyncResponse>(StatusCodes.Status207MultiStatus)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesValidationProblem();


            group.MapPost("/sync", async (
                WorkoutDto dto,
                IWorkoutSyncService syncService,
                HttpContext context,
                CancellationToken ct
                ) =>
            {
                var validationContext = new ValidationContext(dto);
                Validator.ValidateObject(dto, validationContext, validateAllProperties: true);


                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedException("Токен не содержит UserId");

                if (!int.TryParse(userIdClaim.Value, out int userId))
                    throw new UnauthorizedException("Неверный формат UserId в токене");

                var workoutId = await syncService.SyncWorkoutAsync(userId, dto, ct);

                return Results.Ok(new { dto.Identifier, message = "Тренировка успешно синхронизирована" });
            })
                .Produces(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status409Conflict)
                .WithMetadata(new ProducesResponseTypeAttribute(typeof(object), StatusCodes.Status200OK));
        }
    }
}
