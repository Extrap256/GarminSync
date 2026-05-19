namespace Server.API.Extensions
{
    public static class EndpointRouteBuilderExtensions
    {
        //public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
        //{
        //    var group = app.MapGroup("/api/auth");
        //    // group.MapPost("/register", AuthEndpoints.Register);
        //    // group.MapPost("/login", AuthEndpoints.Login);
        //    return app;
        //}

        //public static IEndpointRouteBuilder MapWorkoutEndpoints(this IEndpointRouteBuilder app)
        //{
        //    var group = app.MapGroup("/api/workouts");
        //    // group.MapPost("/sync", WorkoutEndpoints.Sync);
        //    return app;
        //}

        public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app) => app;
        public static IEndpointRouteBuilder MapGroupEndpoints(this IEndpointRouteBuilder app) => app;
        public static IEndpointRouteBuilder MapBotEndpoints(this IEndpointRouteBuilder app) => app;
    }
}
