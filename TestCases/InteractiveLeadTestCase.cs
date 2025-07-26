using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sigortamat.Models;
using Sigortamat.Services;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Sigortamat.TestCases
{
    public class InteractiveLeadTestCase : BaseTestCase
    {
        private Lead _lead;
        private Notification _notification;

        public override string TestName => "Interactive Lead Workflow Test";
        public override string Description => "Lead yaratma və notification workflow-unu user interaction ilə test edir";

        public InteractiveLeadTestCase(IServiceProvider serviceProvider) : base(serviceProvider) { }

        protected override async Task<TestResult> ExecuteTestAsync()
        {
            _logger.LogInformation("🎮 INTERACTIVE LEAD WORKFLOW TEST BAŞLADI");

            // 1. Setup
            await CreateTestDataAsync("10RL033", "0707877878", "NoInsuranceImmediate");
            _lead = await _context.Leads.FirstOrDefaultAsync(l => l.CarNumber == "10RL033");
            if (_lead == null)
            {
                return TestResult.CreateFailure("Lead bazada yaradılmadı.");
            }

            // 2. Call Lead Service
            _logger.LogInformation("🚀 Addım 1: LeadService.CreateNotificationForLeadAsync() çağırılır...");
            var leadService = _serviceProvider.GetRequiredService<LeadService>();
            await leadService.CreateNotificationForLeadAsync(_lead);

            _notification = await _context.Notifications.FirstOrDefaultAsync(n => n.LeadId == _lead.Id);
            if (_notification == null)
            {
                return TestResult.CreateFailure("Notification bazada yaradılmadı.");
            }
            _logger.LogInformation("✅ Notification {NotificationId} yaradıldı və Telegram sorğusu göndərildi.", _notification.Id);

            // 3. User Interaction
            await InteractiveCheck_Approval();

            // 4. Perform Simulation
            await PerformApprovalSimulation();

            // 5. Verify Result in DB
            if (!await CheckApprovalStatusInDb())
            {
                return TestResult.CreateFailure("Təsdiqləmə statusu bazada düzgün əks olunmadı.");
            }

            _logger.LogInformation("🎉 INTERACTIVE TEST UĞURLA TAMAMLANDI!");
            return TestResult.CreateSuccess($"Test '{TestName}' uğurla tamamlandı!");
        }

        private async Task InteractiveCheck_Approval()
        {
            _logger.LogInformation("📱 İNTERAKTİV YOXLAMA: Zəhmət olmasa, Telegram-a gələn Notification (ID: {NotificationId}) üçün 'Təsdiqlə' düyməsini basın.", _notification.Id);
            var input = await GetUserInputAsync("✅ Düyməni basdınızmı? (y/n): ");
            if (input.ToLower() != "y")
            {
                throw new OperationCanceledException("İstifadəçi Telegram təsdiqini ləğv etdi.");
            }
        }

        private async Task PerformApprovalSimulation()
        {
            _logger.LogInformation("🔄 Test rejimində təsdiqləmə simulyasiya edilir...");
            var notificationService = _serviceProvider.GetRequiredService<NotificationService>();
            await notificationService.ApproveAsync(_notification.Id);
            _logger.LogInformation("✅ ApproveAsync (simulyasiya) tamamlandı.");
        }

        private async Task<bool> CheckApprovalStatusInDb()
        {
            await Task.Delay(250); // DB-nin yenilənməsi üçün qısa fasilə
            await _context.Entry(_notification).ReloadAsync(); // Ən son məlumatı al
            
            var success = _notification.Status == "approved";
            if (success)
            {
                _logger.LogInformation("✅ Baza yoxlanışı uğurlu: Notification statusu 'approved'-dur.");
            }
            else
            {
                _logger.LogError("❌ Baza yoxlanışı uğursuz: Gözlənilən status 'approved', mövcud status '{Status}'", _notification.Status);
            }
            return success;
        }
    }
} 