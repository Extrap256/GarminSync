using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Server.API.Endpoints;
using Server.Bot.Endpoints;
using Server.Bot.Vk;
using Server.Bot.Vk.Commands;
using Server.Bot.Vk.Session;
using Server.Common.Exceptions;
using Server.Core.Interfaces;
using Server.Core.Services;
using Server.Infrastructure.Data;
using VkNet;
using VkNet.Abstractions;
using VkNet.Model;

var builder = WebApplication.CreateBuilder();


var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("JWT Secret не найден в appsettings.json");

// Ограничение 100 MB
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 100 * 1024 * 1024;
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(5);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(5);
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        //Для локальных тестов
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters()
        {
            //исправить потом
            ValidateIssuer = false,
            ValidateAudience = false,

            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });
builder.Services.AddAuthorization();


var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' не найден");
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
var dataSource = dataSourceBuilder.Build();

//builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(dataSource, npgsqlOptions =>
    {
        npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
        npgsqlOptions.CommandTimeout(180);
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
    }));

// VK BOT
builder.Services.Configure<VkBotOptions>(builder.Configuration.GetSection(VkBotOptions.SectionName));

builder.Services.AddSingleton<IVkApi>(sp =>
{
    var opts = sp.GetRequiredService<IOptions<VkBotOptions>>().Value;
    var api = new VkApi();
    api.Authorize(new ApiAuthParams { AccessToken = opts.AccessToken });
    return api;
});
builder.Services.AddSingleton<BotSessionStore>();

builder.Services.AddScoped<IVkMessageSender, VkMessageSender>();

builder.Services.AddScoped<VkBotService>();

builder.Services.AddScoped<IBotCommand, StartCommand>();
builder.Services.AddScoped<IBotCommand, RegisterCommand>();
builder.Services.AddScoped<IBotCommand, LoginCommand>();
builder.Services.AddScoped<IBotCommand, LogoutCommand>();
builder.Services.AddScoped<IBotCommand, ReportCommand>();
builder.Services.AddScoped<IBotCommand, ProfileCommand>();
builder.Services.AddScoped<IBotCommand, WorkoutCommand>();
builder.Services.AddScoped<IBotCommand, GroupCommand>();

//Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IWorkoutSyncService, WorkoutSyncService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<IReportService, ReportService>();
//Добавить потом сервисы сюда


var app = builder.Build();

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (ValidationException ex)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = ex.Message });
    }
    catch (UnauthorizedException ex)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = ex.Message });
    }
    catch (NotFoundException ex)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = ex.Message });
    }
    catch (DuplicateWorkoutException ex)
    {
        context.Response.StatusCode = StatusCodes.Status409Conflict;
        await context.Response.WriteAsJsonAsync(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Unhandled exception");
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = "Внутренняя ошибка сервера: " + ex.Message });
    }
});


//if (app.Environment.IsDevelopment())
//{
//    app.UseHttpsRedirection();
//}

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapWorkoutEndpoints();
app.MapUserEndpoints();
app.MapGroupEndpoints();
app.MapReportEndpoints();
app.MapVkBotEndpoints();

app.Run();