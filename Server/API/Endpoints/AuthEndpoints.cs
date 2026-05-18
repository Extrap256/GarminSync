using System.ComponentModel.DataAnnotations;
using Server.API.Models.Auth;
using Server.Core.Interfaces;
using Server.Core.Services;

namespace Server.API.Endpoints
{
    public static class AuthEndpoints
    {
        public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/auth");

            group.MapPost("/register", async (
                RegisterRequest request,
                IAuthService authService,
                CancellationToken ct) =>
            {
                var context = new ValidationContext(request);
                Validator.ValidateObject(request, context, validateAllProperties: true);

                var result = await authService.RegisterAsync(request, ct);
                return Results.Ok(result);
            })
                .Produces<AuthResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status409Conflict);

            group.MapPost("/login", async (
                LoginRequest request,
                IAuthService authService,
                CancellationToken ct) =>
            {
                var context = new ValidationContext(request);
                Validator.ValidateObject(request, context, validateAllProperties: true);

                var result = await authService.LoginAsync(request, ct);
                return Results.Ok(result);
            })
                .Produces<AuthResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status401Unauthorized);
        }
    }
}
