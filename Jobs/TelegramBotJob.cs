using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Microsoft.Extensions.DependencyInjection;
using Sigortamat.Services;

namespace Sigortamat.Jobs
{
    /// <summary>
    /// A Hangfire job to periodically check for and process Telegram updates.
    /// </summary>
    public class TelegramBotJob
    {
        private readonly ILogger<TelegramBotJob> _logger;
        private readonly ITelegramBotClient _botClient;
        private readonly IServiceProvider _serviceProvider;
        private static int _updateOffset = 0;

        public TelegramBotJob(ILogger<TelegramBotJob> logger, ITelegramBotClient botClient, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _botClient = botClient;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Checks for new updates from Telegram and processes them.
        /// </summary>
        public async Task CheckForUpdatesAsync()
        {
            try
            {
                var updates = await _botClient.GetUpdatesAsync(_updateOffset);
                if (!updates.Any())
                {
                    return; // No new updates
                }

                _logger.LogInformation("Received {UpdateCount} new update(s).", updates.Length);

                foreach (var update in updates)
                {
                    await HandleUpdateAsync(update);
                    _updateOffset = update.Id + 1; // Process next update
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while checking for Telegram updates.");
            }
        }
        
        private async Task HandleUpdateAsync(Update update)
        {
            if (update.Type != UpdateType.CallbackQuery || update.CallbackQuery?.Data == null)
                return;

            var callbackQuery = update.CallbackQuery;
            _logger.LogInformation("Processing callback query: Data='{CallbackData}' from user '{Username}'.", callbackQuery.Data, callbackQuery.From.Username);

            using (var scope = _serviceProvider.CreateScope())
            {
                var telegramBotService = scope.ServiceProvider.GetRequiredService<TelegramBotService>();
                await telegramBotService.HandleCallbackQueryAsync(callbackQuery);
            }
        }
    }
} 