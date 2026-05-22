using Server.Core.Interfaces;
using Server.API.Models.User;
using System.Security.Claims;
using Server.Common.Exceptions;

namespace Server.API.Endpoints
{
    public static class UserEndpoints
    {
        public static void MapUserEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/users").RequireAuthorization();

            group.MapGet("/me", async (
                HttpContext context,
                IUserService userService,
                CancellationToken ct = default) =>
            {
                var userId = GetUserId(context);
                var info = await userService.GetUserInfoAsync(userId, ct);
                return Results.Ok(info);
            });

            group.MapPut("/me", async (
                UserUpdateDto dto,
                HttpContext context,
                IUserService userService,
                CancellationToken ct = default
                ) =>
            {
                var userId = GetUserId(context);
                await userService.UpdateUserInfoAsync(userId, dto, ct);
                return Results.NoContent();
            });
        }
        private static int GetUserId(HttpContext context)
        {
            var claim = context.User.FindFirst(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedException("Отсутствует UserId");
            return int.Parse(claim.Value);
        }
    }
}
