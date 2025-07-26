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
    /// NoInsuranceImmediate lead yaratma test case-i
    /// RenewalTrackingService.ProcessInitialPhaseAsync() metodunu test edir
    /// </summary>
    public class NoInsuranceLeadTestCase : TestCaseBase
    {
        private readonly RenewalTrackingService _renewalService;
        private readonly LeadService _leadService;

        public NoInsuranceLeadTestCase(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _renewalService = serviceProvider.GetRequiredService<RenewalTrackingService>();
            _leadService = serviceProvider.GetRequiredService<LeadService>();
        }

        public override string TestName => "NoInsuranceImmediate Lead Test";

        public override string Description => 
            "Avtomobil üçün sığorta məlumatı tapılmadığında avtomatik NoInsuranceImmediate lead yaratmasını test edir";

        protected override string[] GetTestCarNumbers() => new[] { "TEST_NO_INS" };

        protected override async Task ExecuteTestAsync()
        {
            Console.WriteLine("🚀 RenewalTrackingService.StartRenewalTrackingAsync() çağırılır...");
            
            // 1. Renewal tracking başlat
            var trackingId = await _renewalService.StartRenewalTrackingAsync("TEST_NO_INS", "0501234567");
            Console.WriteLine($"✅ Tracking yaradıldı: ID={trackingId}");

            // 2. Fake "No Insurance" job yaradırıq və prosess edirik
            Console.WriteLine("🎭 Fake InsuranceJob yaradılır (sığorta məlumatı olmadan)...");
            
            var fakeJob = new InsuranceJob
            {
                Id = 9999, // Fake ID
                CarNumber = "TEST_NO_INS",
                CheckDate = DateTime.Now,
                InsuranceRenewalTrackingId = trackingId,
                Status = "completed",
                // Burada Company, VehicleBrand, VehicleModel NULL qalır (sığorta yoxdur)
                Company = null,
                VehicleBrand = null,
                VehicleModel = null,
                ResultText = "Məlumat tapılmadı"
            };

            Console.WriteLine("🔄 RenewalTrackingService.ProcessRenewalResultAsync() çağırılır...");
            await _renewalService.ProcessRenewalResultAsync(fakeJob);
            Console.WriteLine("✅ ProcessRenewalResultAsync tamamlandı");
        }

        protected override async Task VerifyResultsAsync()
        {
            Console.WriteLine("🔍 Nəticələr yoxlanılır...");

            await DisplayDatabaseStateAsync();

            // User yaradıldığını yoxla
            var user = await _context.Users.FirstOrDefaultAsync(u => u.CarNumber == "TEST_NO_INS");
            if (user == null)
                throw new Exception("User yaradılmadı!");
            Console.WriteLine($"✅ User yaradıldı: ID={user.Id}");

            // Lead yaradıldığını yoxla
            var lead = await _context.Leads.FirstOrDefaultAsync(l => l.CarNumber == "TEST_NO_INS");
            if (lead == null)
                throw new Exception("Lead yaradılmadı!");
            if (lead.LeadType != "NoInsuranceImmediate")
                throw new Exception($"Lead tipi səhvdir: {lead.LeadType}");
            Console.WriteLine($"✅ NoInsuranceImmediate lead yaradıldı: ID={lead.Id}");

            // Notification yaradıldığını yoxla
            var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.LeadId == lead.Id);
            if (notification == null)
                throw new Exception("Notification yaradılmadı!");
            if (notification.Status != "pending")
                throw new Exception($"Notification status səhvdir: {notification.Status}");
            Console.WriteLine($"✅ Notification yaradıldı: ID={notification.Id}, Status={notification.Status}");

            // Tracking completed olduğunu yoxla
            var tracking = await _context.InsuranceRenewalTracking.FirstOrDefaultAsync(t => t.UserId == user.Id);
            if (tracking == null)
                throw new Exception("Tracking tapılmadı!");
            if (tracking.CurrentPhase != "Completed")
                throw new Exception($"Tracking phase səhvdir: {tracking.CurrentPhase}");
            Console.WriteLine($"✅ Tracking completed: Phase={tracking.CurrentPhase}");

            Console.WriteLine("🎯 Gözlənilən bütün şərtlər ödənildi!");
        }
    }
} 