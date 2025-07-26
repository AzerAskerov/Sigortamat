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
    /// Notification approval test case - NotificationService.ApproveAsync test edir
    /// </summary>
    public class NotificationApprovalTest : BaseTestCase
    {
        private readonly NotificationService _notificationService;

        public NotificationApprovalTest(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _notificationService = serviceProvider.GetRequiredService<NotificationService>();
        }

        public override string TestName => "Notification Approval Test";

        public override string Description => 
            "Notification approval prosesini test edir";

        protected override async Task ExecuteTestAsync()
        {
            // 1. Test data təmizlə
            await CleanupTestDataAsync();

            // 2. Test data yarat
            var notificationId = await CreateTestDataAsync();

            // 3. NotificationService.ApproveAsync çağır
            Console.WriteLine($"🔄 NotificationService.ApproveAsync({notificationId}) çağırılır...");
            await _notificationService.ApproveAsync(notificationId);
            Console.WriteLine("✅ ApproveAsync tamamlandı");

            // 4. Nəticəni yoxla
            await VerifyApprovalAsync(notificationId);
        }

        private async Task CleanupTestDataAsync()
        {
            var notifications = await _context.Notifications
                .Where(n => n.Lead.CarNumber == "APPROVAL_TEST")
                .ToListAsync();
            _context.Notifications.RemoveRange(notifications);

            var leads = await _context.Leads
                .Where(l => l.CarNumber == "APPROVAL_TEST")
                .ToListAsync();
            _context.Leads.RemoveRange(leads);

            var users = await _context.Users
                .Where(u => u.CarNumber == "APPROVAL_TEST")
                .ToListAsync();
            _context.Users.RemoveRange(users);

            await _context.SaveChangesAsync();
            Console.WriteLine("🗑️ Test data təmizləndi");
        }

        private async Task<int> CreateTestDataAsync()
        {
            Console.WriteLine("🚀 Test data yaradılır...");

            // User
            var user = new User
            {
                CarNumber = "APPROVAL_TEST",
                PhoneNumber = "0507654321",
                NotificationEnabled = true,
                CreatedAt = DateTime.Now
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Lead
            var lead = new Lead
            {
                UserId = user.Id,
                CarNumber = user.CarNumber,
                LeadType = "NoInsuranceImmediate",
                Notes = "Test approval",
                CreatedAt = DateTime.Now
            };
            _context.Leads.Add(lead);
            await _context.SaveChangesAsync();

            // Notification
            var notification = new Notification
            {
                LeadId = lead.Id,
                Channel = "wa",
                Message = "Test approval message",
                Status = "pending",
                CreatedAt = DateTime.Now
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            Console.WriteLine($"✅ Test data yaradıldı: User={user.Id}, Lead={lead.Id}, Notification={notification.Id}");
            return notification.Id;
        }

        private async Task VerifyApprovalAsync(int notificationId)
        {
            Console.WriteLine("🔍 Approval nəticəsi yoxlanılır...");

            var notification = await _context.Notifications.FindAsync(notificationId);
            
            if (notification == null)
                throw new Exception("Notification tapılmadı!");
            
            if (notification.Status != "approved")
                throw new Exception($"Status səhvdir: {notification.Status}, gözlənilən: approved");
            
            if (notification.ApprovedAt == null)
                throw new Exception("ApprovedAt tarixi set edilməyib!");
            
            Console.WriteLine($"✅ Notification approved: Status={notification.Status}");
            Console.WriteLine($"✅ ApprovedAt: {notification.ApprovedAt:yyyy-MM-dd HH:mm}");
            Console.WriteLine("🎯 Test uğurludur!");
        }
    }
} 