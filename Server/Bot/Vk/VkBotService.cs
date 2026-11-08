using System.Text.Json;
using Microsoft.Extensions.Options;
using Server.Bot.Vk.Commands;
using Server.Bot.Vk.Session;
using VkNet;
using VkNet.Abstractions;
using VkNet.Model;

namespace Server.Bot.Vk
{
    public class VkBotService
    {
        private readonly IOptions<VkBotOptions> _options;
        private readonly ILogger<VkBotService> _logger;
        private readonly IEnumerable<IBotCommand> _commands;
        private readonly IVkMessageSender _messageSender;
        private readonly BotSessionStore _sessionStore;

        public VkBotService(
            IOptions<VkBotOptions> options,
            ILogger<VkBotService> logger,
            IEnumerable<IBotCommand> commands,
            IVkMessageSender messageSender,
            BotSessionStore sessionStore)
        {
            _options = options;
            _logger = logger;
            _commands = commands;
            _messageSender = messageSender;
            _sessionStore = sessionStore;
        }

        public async Task HandleEventAsync(string jsonBody)
        {
            var doc = JsonDocument.Parse(jsonBody);
            var root = doc.RootElement;
            var type = root.GetProperty("type").GetString();

            if (type != "message_new")
                return;

            var objectEl = root.GetProperty("object");
            var messageEl = objectEl.TryGetProperty("message", out var msg) ? msg : objectEl;
            var fromId = messageEl.GetProperty("from_id").GetInt64();
            var text = messageEl.TryGetProperty("text", out var t) ? t.GetString() ?? "" : "";

            await HandleMessageAsync(fromId, text);
        }

        private async Task HandleMessageAsync(long vkUserId, string text)
        {
            var trimmed = text.Trim();

            // === 1. /cancel — отмена любого диалога ===
            if (trimmed.Equals("/cancel", StringComparison.OrdinalIgnoreCase))
            {
                _sessionStore.Remove(vkUserId);
                await _messageSender.SendMessageAsync(vkUserId, "Действие отменено");
                return;
            }

            // === 2. Проверяем активную сессию (если сообщение не команда) ===
            var session = _sessionStore.Get(vkUserId);
            if (session != null && !trimmed.StartsWith("/"))
            {
                var interactiveCmd = _commands
                    .OfType<IInteractiveBotCommand>()
                    .FirstOrDefault(c => c.GetType().Name == session.CommandName);

                if (interactiveCmd != null)
                {
                    try
                    {
                        await interactiveCmd.HandleStepAsync(vkUserId, session, trimmed);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Ошибка в интерактивной команде {Cmd}", session.CommandName);
                        await _messageSender.SendMessageAsync(vkUserId, $"Ошибка: {ex.Message}");
                    }
                    return;
                }
                // Если команда не найдена — сбрасываем сессию
                _sessionStore.Remove(vkUserId);
            }

            // === 3. Ищем обычную команду ===
            var command = _commands.FirstOrDefault(c => c.CanHandle(trimmed));

            if (command == null)
            {
                await _messageSender.SendMessageAsync(vkUserId,
                    "Неизвестная команда\n\n" + StartCommand.GetHelpMessage());
                return;
            }

            try
            {
                await command.ExecuteAsync(vkUserId, trimmed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выполнении команды {Command}", command.GetType().Name);
                await _messageSender.SendMessageAsync(vkUserId, $"Ошибка: {ex.Message}");
            }
        }
    }
}
