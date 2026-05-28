using System.Text.Json;
using Microsoft.Extensions.Options;
using Server.Bot.Vk;
using Server.Core.Interfaces;

namespace Server.Bot.Endpoints
{
    public static class VkCallbackEndpoint
    {
        public static void MapVkBotEndpoints(this IEndpointRouteBuilder app)
        {
            // ВАЖНО: этот endpoint должен быть ДО всех middleware проверок
            app.MapPost("/api/bot/vk/callback", async (
                HttpContext context,
                VkBotService botService,
                IOptions<VkBotOptions> options,
                ILogger<VkBotService> logger) =>
            {
                // === 1. Логируем всё входящее ===
                logger.LogInformation("=== VK Callback Request ===");
                logger.LogInformation("Path: {Path}", context.Request.Path);
                logger.LogInformation("Method: {Method}", context.Request.Method);
                logger.LogInformation("ContentType: {ContentType}", context.Request.ContentType);

                // === 2. Читаем тело ===
                string body;
                using (var reader = new StreamReader(context.Request.Body))
                {
                    body = await reader.ReadToEndAsync();
                }
                logger.LogInformation("Body: {Body}", body);

                if (string.IsNullOrWhiteSpace(body))
                {
                    logger.LogError("Пустое тело запроса");
                    return Results.BadRequest("Empty body");
                }

                // === 3. Парсим JSON ===
                JsonDocument doc;
                try
                {
                    doc = JsonDocument.Parse(body);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Невалидный JSON");
                    return Results.BadRequest("Invalid JSON");
                }

                var root = doc.RootElement;

                if (!root.TryGetProperty("type", out var typeEl))
                {
                    logger.LogError("Нет поля 'type'");
                    return Results.BadRequest("Missing 'type'");
                }

                var type = typeEl.GetString();
                logger.LogInformation("Event type: {Type}", type);

                // === 4. CONFIRMATION — самое важное, обрабатываем ПЕРВЫМ ===
                if (type == "confirmation")
                {
                    var code = options.Value.ConfirmationCode;
                    logger.LogInformation("Отправляем confirmation code: '{Code}'", code);

                    // ВАЖНО: именно Content с text/plain, без кавычек!
                    context.Response.ContentType = "text/plain; charset=utf-8";
                    context.Response.StatusCode = 200;
                    await context.Response.WriteAsync(code);
                    return Results.Empty;  // Тело уже записано вручную
                }

                // === 5. Проверка secret_key для остальных событий ===
                if (!string.IsNullOrEmpty(options.Value.SecretKey))
                {
                    if (!root.TryGetProperty("secret", out var secretEl) ||
                        secretEl.GetString() != options.Value.SecretKey)
                    {
                        logger.LogWarning("Invalid secret. Expected: {Expected}, Got: {Got}",
                            options.Value.SecretKey,
                            root.TryGetProperty("secret", out var s) ? s.GetString() : "<missing>");
                        return Results.StatusCode(403);
                    }
                }

                // === 6. Обработка события ===
                try
                {
                    await botService.HandleEventAsync(body);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Ошибка при обработке события");
                }

                // === 7. Всегда возвращаем "ok" ===
                context.Response.ContentType = "text/plain; charset=utf-8";
                context.Response.StatusCode = 200;
                await context.Response.WriteAsync("ok");
                return Results.Empty;
            })
            .AllowAnonymous()
            .WithMetadata(new Microsoft.AspNetCore.Routing.SuppressLinkGenerationMetadata());
        }
    }
}
