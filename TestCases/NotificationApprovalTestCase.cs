using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sigortamat.Models;
using Sigortamat.Services;

namespace Sigortamat.TestCases
{
    /// <summary>
    /// Notification approval prosesi test case-i
    /// NotificationService.ApproveAsync() metodunu test edir
    /// </summary>
    public class NotificationApprovalTestCase : TestCaseBase
    {
        private readonly NotificationService _notificationService;
        private readonly LeadService _leadService;

        public NotificationApprovalTestCase(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _notificationService = serviceProvider.GetRequiredService<NotificationService>();
            _leadService = serviceProvider.GetRequiredService<LeadService>();
        }

        public override string TestName => "Notification Approval Test";

        public override string Description => 
            "Notification-un admin tərəfindən approve edilməsi və status dəyişikliklərini test edir";

        protected override string[] GetTestCarNumbers() => new[] { "TEST_APPROVAL" };

        protected override async Task ExecuteTestAsync()
        {
            Console.WriteLine("🚀 Notification approval workflow test başlayır...");
            
            // 1. Test user yaradırıq
            var user = new User
            {
                CarNumber = "TEST_APPROVAL",
                PhoneNumber = "0507654321",
                NotificationEnabled = true,
                CreatedAt = DateTime.Now
            };
            
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            Console.WriteLine($"✅ Test User yaradıldı: ID={user.Id}");

            // 2. Test lead yaradırıq
            var lead = new Lead
            {
                UserId = user.Id,
                CarNumber = user.CarNumber,
                LeadType = "NoInsuranceImmediate",
                Notes = "Test approval workflow",
                CreatedAt = DateTime.Now
            };
            
            _context.Leads.Add(lead);
            await _context.SaveChangesAsync();
            Console.WriteLine($"✅ Test Lead yaradıldı: ID={lead.Id}");

            // 3. LeadService.CreateNotificationForLeadAsync çağırırıq
            Console.WriteLine("🔄 LeadService.CreateNotificationForLeadAsync() çağırılır...");
            await _leadService.CreateNotificationForLeadAsync(lead);
            Console.WriteLine("✅ Notification yaradıldı və Telegram request göndərildi (log-da görünür)");

            // 4. Yaradılan notification-u tapırıq
            var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.LeadId == lead.Id);
            if (notification == null)
                throw new Exception("Notification yaradılmadı!");
            
            Console.WriteLine($"✅ Notification tapıldı: ID={notification.Id}, Status={notification.Status}");

            // 5. Admin approval simulation edirik
            Console.WriteLine($"🔄 NotificationService.ApproveAsync({notification.Id}) çağırılır...");
            await _notificationService.ApproveAsync(notification.Id);
            Console.WriteLine("✅ Notification approve edildi");

            // 6. Notification status-u "sent"-ə keçiririk (WhatsApp job simulation)
            Console.WriteLine($"🔄 NotificationService.MarkAsSentAsync({notification.Id}) çağırılır...");
            await _notificationService.MarkAsSentAsync(notification.Id);
            Console.WriteLine("✅ Notification sent kimi qeyd edildi");

            // 7. Error scenario test edirik - yeni notification yaradıb error status veririk
            var errorNotification = new Notification
            {
                LeadId = lead.Id,
                Channel = "wa",
                Message = "Test error notification",
                Status = "pending",
                CreatedAt = DateTime.Now
            };
            
            _context.Notifications.Add(errorNotification);
            await _context.SaveChangesAsync();

            Console.WriteLine($"🔄 NotificationService.MarkAsErrorAsync({errorNotification.Id}) çağırılır...");
            await _notificationService.MarkAsErrorAsync(errorNotification.Id, "Test error message");
            Console.WriteLine("✅ Notification error kimi qeyd edildi");
        }

        protected override async Task VerifyResultsAsync()
        {
            Console.WriteLine("🔍 Nəticələr yoxlanılır...");

            await DisplayDatabaseStateAsync();

            var notifications = await _context.Notifications
                .Where(n => n.Lead.CarNumber == "TEST_APPROVAL")
                .ToListAsync();

            if (notifications.Count < 2)
                throw new Exception($"Gözlənilən 2 notification, tapılan: {notifications.Count}");

            // İlk notification (approved -> sent)
            var approvedNotification = notifications.FirstOrDefault(n => n.Status == "sent");
            if (approvedNotification == null)
                throw new Exception("Approved və sent notification tapılmadı!");
            
            if (approvedNotification.ApprovedAt == null)
                throw new Exception("ApprovedAt tarixi set edilməyib!");
            
            if (approvedNotification.SentAt == null)
                throw new Exception("SentAt tarixi set edilməyib!");
            
            Console.WriteLine($"✅ Approved notification: ID={approvedNotification.Id}");
            Console.WriteLine($"   ApprovedAt: {approvedNotification.ApprovedAt:yyyy-MM-dd HH:mm}");
            Console.WriteLine($"   SentAt: {approvedNotification.SentAt:yyyy-MM-dd HH:mm}");

            // İkinci notification (error)
            var errorNotification = notifications.FirstOrDefault(n => n.Status == "error");
            if (errorNotification == null)
                throw new Exception("Error notification tapılmadı!");
            
            Console.WriteLine($"✅ Error notification: ID={errorNotification.Id}, Status={errorNotification.Status}");

            // Telegram log mesajlarını yoxla
            Console.WriteLine("📝 Telegram bot log mesajları console-da görünməlidir:");
            Console.WriteLine("   - 'TELEGRAM APPROVAL REQUEST' mesajı");
            Console.WriteLine("   - 'TO IMPLEMENT: Send to admin chat' mesajı");

            Console.WriteLine("🎯 Gözlənilən bütün şərtlər ödənildi!");
        }
    }
} 