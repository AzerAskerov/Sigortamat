using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sigortamat.Data;
using Sigortamat.Models;
using Sigortamat.Services;
using System;
using System.Threading.Tasks;

namespace Sigortamat.TestCases
{
    /// <summary>
    /// This test case only creates a lead and a notification, then exits.
    /// It's designed for end-to-end testing where the main application will handle the approval.
    /// </summary>
    public class CreateLeadOnlyTestCase : BaseTestCase
    {
        public override string TestName => "Create Lead & Notification Only";
        public override string Description => "Yalnız yeni bir Lead və Notification yaradır və çıxır. Real rejimdə təsdiqləməni yoxlamaq üçün istifadə olunur.";

        public CreateLeadOnlyTestCase(IServiceProvider serviceProvider) : base(serviceProvider) { }

        protected override async Task<TestResult> ExecuteTestAsync()
        {
            var leadService = _serviceProvider.GetRequiredService<LeadService>();
            
            // 1. Create Test Data
            _logger.LogInformation("🔧 Test üçün məlumatlar yaradılır...");
            var (user, lead) = await CreateTestDataAsync("10RL033", "0707877878", "NoInsuranceImmediate");
            _logger.LogInformation("✅ Test məlumatları yaradıldı: User ID={UserId}, Lead ID={LeadId}", user.Id, lead.Id);

            // 2. Call the service to create the notification
            _logger.LogInformation("🚀 LeadService.CreateNotificationForLeadAsync() çağırılır...");
            await leadService.CreateNotificationForLeadAsync(lead);
            _logger.LogInformation("✅ Telegram-a təsdiq sorğusu göndərildi. Notification ID: {NotificationId}", lead.Id);

            return TestResult.CreateSuccess($"Test uğurla tamamlandı. Telegram-a Notification ID {lead.Id} üçün təsdiq mesajı göndərildi. İndi 'dotnet run' ilə əsas proqramı işə salıb düyməni basa bilərsiniz.");
        }
    }
} 