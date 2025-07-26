using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sigortamat.Data;
using Sigortamat.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Exceptions;

namespace Sigortamat.Services
{
    public class TelegramBotService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ILogger<TelegramBotService> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IServiceProvider _serviceProvider;
        private const long AdminChatId = 1762884854;

        public TelegramBotService(ITelegramBotClient botClient, ILogger<TelegramBotService> logger, ApplicationDbContext context, IServiceProvider serviceProvider)
        {
            _botClient = botClient;
            _logger = logger;
            _context = context;
            _serviceProvider = serviceProvider;
        }

        public async Task SendApprovalRequestAsync(Notification notification)
        {
            _logger.LogInformation("Sending Telegram approval request for Notification ID: {NotificationId}", notification.Id);

            var lead = await _context.Leads
                .Include(l => l.User)
                .FirstOrDefaultAsync(l => l.Id == notification.LeadId);

            if (lead == null || lead.User == null)
            {
                _logger.LogError("Lead or User not found for Notification ID: {NotificationId}", notification.Id);
                return;
            }

            var text = $"🚗 **{lead.CarNumber}**\n" +
                       $"📋 Lead tip: **{lead.LeadType}**\n" +
                       $"👤 Müştəri: {lead.User.PhoneNumber ?? "N/A"}\n\n" +
                       $"📱 Müştəriyə göndəriləcək WhatsApp mesajı:\n" +
                       $"```\n{notification.Message}\n```";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                InlineKeyboardButton.WithCallbackData("✅ Təsdiqlə", $"approve:{notification.Id}"),
                InlineKeyboardButton.WithCallbackData("❌ Rədd et", $"reject:{notification.Id}")
            });

            try
            {
                await _botClient.SendTextMessageAsync(
                    chatId: AdminChatId,
                    text: text,
                    // parseMode: ParseMode.Markdown, // <-- Temporarily disable for testing
                    replyMarkup: keyboard
                );
                _logger.LogInformation("Successfully sent Telegram message for Notification ID: {NotificationId}", notification.Id);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Failed to send Telegram message for Notification ID: {NotificationId}", notification.Id);
            }
        }

        /// <summary>
        /// Handles callback queries from the TelegramBotJob.
        /// </summary>
        public async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery)
        {
            var data = callbackQuery.Data;
            if (string.IsNullOrEmpty(data)) return;

            var dataParts = data.Split(':');
            if (dataParts.Length != 2 || !int.TryParse(dataParts[1], out var notificationId))
            {
                _logger.LogWarning("Invalid callback data format received: {CallbackData}", data);
                return;
            }

            var action = dataParts[0];

            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();
                    
                    if (action == "approve")
                    {
                        await notificationService.ApproveAsync(notificationId);
                        await EditTelegramMessageAsync(callbackQuery, "✅ Təsdiqləndi");
                    }
                    else if (action == "reject")
                    {
                        await notificationService.RejectAsync(notificationId);
                        await EditTelegramMessageAsync(callbackQuery, "❌ Rədd edildi");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing callback for Notification ID {NotificationId}", notificationId);
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Xəta baş verdi", showAlert: true);
            }
        }

        private async Task EditTelegramMessageAsync(CallbackQuery callbackQuery, string resultText)
        {
            // 1) Try to answer callback (ignore if too old)
            try
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, resultText);
            }
            catch (ApiRequestException ex) when (ex.Message.Contains("query is too old"))
            {
                _logger.LogWarning("AnswerCallbackQuery failed (too old) for callback {CallbackId}. Continuing to edit message...", callbackQuery.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unexpected error when answering callback query {CallbackId}", callbackQuery.Id);
            }

            // 2) Remove inline keyboard so admin can't click again.
            try
            {
                await _botClient.EditMessageReplyMarkupAsync(
                    chatId: callbackQuery.Message.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    replyMarkup: null);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove keyboard for callback {CallbackId}", callbackQuery.Id);
            }

            // 3) Send a new status message so admin sees the outcome clearly.
            try
            {
                await _botClient.SendTextMessageAsync(
                    chatId: callbackQuery.Message.Chat.Id,
                    text: $"📋 Notification {resultText} (ID: {callbackQuery.Data?.Split(':').LastOrDefault()})",
                    replyToMessageId: callbackQuery.Message.MessageId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send status update for callback {CallbackId}", callbackQuery.Id);
            }
        }
    }
} 