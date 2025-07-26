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
    /// Lead yaratma test case - LeadService.CreateNotificationForLeadAsync test edir
    /// </summary>
    public class LeadCreationTestCase : BaseTestCase
    {
        private readonly LeadService _leadService;

        public LeadCreationTestCase(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _leadService = serviceProvider.GetRequiredService<LeadService>();
        }

        public override string TestName => "Lead Creation Test";

        public override string Description => 
            "Lead yaratma və notification yaratma funksionallığını test edir";

        protected override async Task ExecuteTestAsync()
        {
            // 1. Test data təmizlə
            await CleanupTestDataAsync();

            // 2. User yarat
            Console.WriteLine("🚀 User yaradılır...");
            var user = new User
            {
                CarNumber = "LEAD_TEST",
                PhoneNumber = "0501234567",
                NotificationEnabled = true,
                CreatedAt = DateTime.Now
            };
            
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            Console.WriteLine($"✅ User yaradıldı: ID={user.Id}");

            // 3. Lead yarat
            Console.WriteLine("🚀 Lead yaradılır...");
            var lead = new Lead
            {
                UserId = user.Id,
                CarNumber = user.CarNumber,
                LeadType = "NoInsuranceImmediate",
                Notes = "Test lead",
                CreatedAt = DateTime.Now
            };
            
            _context.Leads.Add(lead);
            await _context.SaveChangesAsync();
            Console.WriteLine($"✅ Lead yaradıldı: ID={lead.Id}");

            // 4. LeadService.CreateNotificationForLeadAsync çağır
            Console.WriteLine("🔄 LeadService.CreateNotificationForLeadAsync() çağırılır...");
            await _leadService.CreateNotificationForLeadAsync(lead);
            Console.WriteLine("✅ CreateNotificationForLeadAsync tamamlandı");

            // 5. Nəticəni yoxla
            await VerifyResultsAsync(lead.Id);
        }

        private async Task CleanupTestDataAsync()
        {
            var notifications = await _context.Notifications
                .Where(n => n.Lead.CarNumber == "LEAD_TEST")
                .ToListAsync();
            _context.Notifications.RemoveRange(notifications);

            var leads = await _context.Leads
                .Where(l => l.CarNumber == "LEAD_TEST")
                .ToListAsync();
            _context.Leads.RemoveRange(leads);

            var users = await _context.Users
                .Where(u => u.CarNumber == "LEAD_TEST")
                .ToListAsync();
            _context.Users.RemoveRange(users);

            await _context.SaveChangesAsync();
            Console.WriteLine("🗑️ Test data təmizləndi");
        }

        private async Task VerifyResultsAsync(int leadId)
        {
            Console.WriteLine("🔍 Nəticələr yoxlanılır...");

            // Notification yaradıldığını yoxla
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.LeadId == leadId);
            
            if (notification == null)
                throw new Exception("Notification yaradılmadı!");
            
            if (notification.Status != "pending")
                throw new Exception($"Notification status səhvdir: {notification.Status}");
            
            Console.WriteLine($"✅ Notification yaradıldı: ID={notification.Id}, Status={notification.Status}");
            Console.WriteLine($"📝 Mesaj: {notification.Message.Substring(0, Math.Min(50, notification.Message.Length))}...");
            Console.WriteLine("🎯 Test uğurludur!");
        }
    }
} 