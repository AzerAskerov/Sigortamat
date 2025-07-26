using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sigortamat.Data;
using Sigortamat.Models;

namespace Sigortamat.Services
{
    public class LeadService
    {
        private readonly ApplicationDbContext _context;
        private readonly TelegramBotService _telegramBot;
        private readonly ILogger<LeadService> _logger;

        public LeadService(ApplicationDbContext context, TelegramBotService telegramBot, ILogger<LeadService> logger)
        {
            _context = context;
            _telegramBot = telegramBot;
            _logger = logger;
        }

        /// <summary>
        /// Lead üçün notification yaradır və Telegram bot ilə admin-ə göndərir
        /// </summary>
        public async Task CreateNotificationForLeadAsync(Lead lead)
        {
            try
            {
                var user = await _context.Users.FindAsync(lead.UserId);
                if (user == null)
                {
                    _logger.LogError("User not found for lead: {LeadId}", lead.Id);
                    return;
                }

                var message = GenerateMessageForLead(lead, user);
                
                var notification = new Notification
                {
                    LeadId = lead.Id,
                    Channel = "wa",
                    Message = message,
                    Status = "pending"
                };
                
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Created notification {NotificationId} for lead {LeadId} with message: {Message}", 
                    notification.Id, lead.Id, message);

                // Telegram bot ilə admin-ə göndər
                await _telegramBot.SendApprovalRequestAsync(notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification for lead: {LeadId}", lead.Id);
                throw;
            }
        }

        /// <summary>
        /// Lead tipinə görə mesaj yaradır
        /// </summary>
        private string GenerateMessageForLead(Lead lead, User user)
        {
            return lead.LeadType switch
            {
                "NoInsuranceImmediate" => $"⚠️ {lead.CarNumber} nömrəli avtomobilinizin sığortası yoxdur!\n" +
                                          $"📋 Sığorta etmək üçün bizimlə əlaqə saxlayın:\n" +
                                          $"📞 +994 50 123 45 67\n" +
                                          $"💼 Ən yaxşı qiymətə sığorta təklifi alın!",
                                          
                "RenewalWindow" => $"📅 {lead.CarNumber} nömrəli avtomobilinizin sığorta yenilənmə tarixi yaxınlaşır!\n" +
                                   $"📋 Yeni sığorta üçün bizimlə əlaqə saxlayın:\n" +
                                   $"📞 +994 50 123 45 67\n" +
                                   $"💼 Ən sərfəli qiymətlərlə sığorta təklifi alın!",
                                   
                "CompanyChange" => $"🔄 {lead.CarNumber} nömrəli avtomobilinizin sığorta şirkəti dəyişib!\n" +
                                   $"📋 Yeni sığorta təklifi üçün bizimlə əlaqə saxlayın:\n" +
                                   $"📞 +994 50 123 45 67\n" +
                                   $"💼 Daha yaxşı şərtlərlə sığorta seçimi edin!",
                                   
                _ => $"🔄 {lead.CarNumber} nömrəli avtomobiliniz üçün sığorta təklifi!\n" +
                     $"📋 Ətraflı məlumat üçün bizimlə əlaqə saxlayın:\n" +
                     $"📞 +994 50 123 45 67"
            };
        }

        /// <summary>
        /// Bütün lead-ləri əldə edir
        /// </summary>
        public async Task<List<Lead>> GetLeadsAsync(string? leadType = null)
        {
            var query = _context.Leads
                .Include(l => l.User)
                .Include(l => l.Notifications)
                .AsQueryable();

            if (!string.IsNullOrEmpty(leadType))
            {
                query = query.Where(l => l.LeadType == leadType);
            }

            return await query.OrderByDescending(l => l.CreatedAt).ToListAsync();
        }

        /// <summary>
        /// Lead-i "converted" statusuna keçirir
        /// </summary>
        public async Task ConvertLeadAsync(int leadId)
        {
            var lead = await _context.Leads.FindAsync(leadId);
            if (lead == null)
            {
                _logger.LogError("Lead not found: {LeadId}", leadId);
                return;
            }

            lead.IsConverted = true;
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Lead {LeadId} converted successfully", leadId);
        }

        /// <summary>
        /// Lead statistikalarını əldə edir
        /// </summary>
        public async Task<object> GetLeadStatisticsAsync()
        {
            var stats = await _context.Leads
                .GroupBy(l => l.LeadType)
                .Select(g => new
                {
                    LeadType = g.Key,
                    TotalLeads = g.Count(),
                    ConvertedLeads = g.Count(l => l.IsConverted),
                    ConversionRate = g.Count(l => l.IsConverted) * 100.0 / g.Count()
                })
                .ToListAsync();

            return stats;
        }
    }
} 