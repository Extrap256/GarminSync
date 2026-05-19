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
