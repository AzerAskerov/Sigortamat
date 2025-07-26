using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sigortamat.Data;
using Sigortamat.Models;

namespace Sigortamat.Services
{
    public class NotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NotificationService> _logger;
        private readonly WhatsAppJobRepository _whatsappJobRepository;

        public NotificationService(ApplicationDbContext context, ILogger<NotificationService> logger, WhatsAppJobRepository whatsappJobRepository)
        {
            _context = context;
            _logger = logger;
            _whatsappJobRepository = whatsappJobRepository;
        }

        public async Task ApproveAsync(int notificationId)
        {
            var notification = await _context.Notifications
                .Include(n => n.Lead)
                .ThenInclude(l => l.User)
                .FirstOrDefaultAsync(n => n.Id == notificationId);

            if (notification == null)
            {
                _logger.LogWarning("ApproveAsync: Notification {NotificationId} not found.", notificationId);
                return;
            }

            if (notification.Status != "pending")
            {
                _logger.LogInformation("Notification {NotificationId} already processed. Status: {Status}", notificationId, notification.Status);
                return;
            }

            notification.Status = "approved";
            notification.ApprovedAt = DateTime.UtcNow;
            
            // --- CRITICAL FIX: Create a job for the old WhatsAppJob system ---
            if (notification.Lead?.User?.PhoneNumber != null)
            {
                _whatsappJobRepository.CreateWhatsAppJob(
                    notification.Lead.User.PhoneNumber, 
                    notification.Message, 
                    notification.Id
                );
            }
            else
            {
                _logger.LogWarning("Cannot create WhatsApp job for Notification {NotificationId} because phone number is missing.", notificationId);
            }
            // --- End of critical fix ---

            await _context.SaveChangesAsync();
            _logger.LogInformation("Notification {NotificationId} approved and job created for WhatsApp.", notificationId);
        }

        public async Task MarkAsSentAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null && notification.Status == "approved")
            {
                notification.Status = "sent";
                notification.SentAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Notification {NotificationId} marked as sent.", notificationId);
            }
        }

        public async Task RejectAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null && notification.Status == "pending")
            {
                notification.Status = "rejected";
                await _context.SaveChangesAsync();
                _logger.LogInformation("Notification {NotificationId} rejected.", notificationId);
            }
        }

        public async Task MarkAsErrorAsync(int notificationId, string errorMessage)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.Status = "error";
                // Optionally log the error message to a new field in the Notification table
                await _context.SaveChangesAsync();
                _logger.LogError("Notification {NotificationId} marked as error. Reason: {Error}", notificationId, errorMessage);
            }
        }
    }
} 