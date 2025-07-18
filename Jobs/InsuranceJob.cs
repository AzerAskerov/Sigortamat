using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Hangfire;
using Sigortamat.Services;
using Microsoft.Extensions.Configuration;

namespace Sigortamat.Jobs
{
    /// <summary>
    /// Sığorta yoxlama job-u - Yeni normallaşdırılmış sistem
    /// </summary>
    public class InsuranceJob
    {
        private readonly InsuranceService _insuranceService;
        private readonly IConfiguration _configuration;

        public InsuranceJob()
        {
            // Konfiqurasiyani oxu
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            _configuration = builder.Build();
            
            var useSimulation = _configuration.GetValue<bool>("InsuranceSettings:UseSimulation");
            _insuranceService = new InsuranceService(useSimulation);
            
            Console.WriteLine($"🔧 Insurance Service rejimi: {(useSimulation ? "Simulasiya" : "Real Selenium")}");
        }

        /// <summary>
        /// Yeni sığorta yoxlama job-u - hər dəqiqə işləyir
        /// </summary>
        [Queue("insurance")]
        public async Task ProcessInsuranceQueue()
        {
            Console.WriteLine("\n🚗 SİGORTA JOB BAŞLADI (Yeni sistem)");
            Console.WriteLine("=".PadRight(50, '='));
            
            var pendingJobs = InsuranceJobRepository.GetPendingInsuranceJobs(5);
            
            if (pendingJobs.Count == 0)
            {
                Console.WriteLine("📋 Proses olunacaq sığorta işi yoxdur");
                return;
            }

            Console.WriteLine($"📋 {pendingJobs.Count} sığorta işi tapıldı");

            foreach (var job in pendingJobs)
            {
                var stopwatch = Stopwatch.StartNew();
                try
                {
                    Console.WriteLine($"\n🔄 İşlənir: {job.CarNumber} (Queue ID: {job.QueueId})");
                    
                    // Queue-u processing kimi işarələ
                    QueueRepository.MarkAsProcessing(job.QueueId);
                    
                    var result = await _insuranceService.CheckInsuranceAsync(job.CarNumber);
                    stopwatch.Stop();
                    
                    // Real məlumatları parse et və yenilə
                    string vehicleBrand = result.VehicleBrand ?? "";
                    string vehicleModel = result.VehicleModel ?? "";
                    string resultText = result.FullResultText ?? "";
                    
                    // DEBUG: Real məlumatları göstər
                    Console.WriteLine($"🔧 DEBUG - Brand: {vehicleBrand}, Model: {vehicleModel}");
                    
                    // Nəticəni yenilə
                    InsuranceJobRepository.UpdateInsuranceResult(
                        job.QueueId,
                        result.Status,
                        result.Company,
                        (int)stopwatch.ElapsedMilliseconds,
                        vehicleBrand,
                        vehicleModel,
                        resultText
                    );
                    
                    // Queue-u tamamlanmış kimi işarələ
                    QueueRepository.MarkAsCompleted(job.QueueId);
                    Console.WriteLine($"✅ Tamamlandı: {job.CarNumber} ({stopwatch.ElapsedMilliseconds}ms)");
                    
                    // Rate limiting
                    await Task.Delay(1000);
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    QueueRepository.MarkAsFailed(job.QueueId, ex.Message);
                    Console.WriteLine($"❌ Xəta: {job.CarNumber} - {ex.Message}");
                }
            }

            Console.WriteLine($"✅ Sığorta job tamamlandı: {pendingJobs.Count} element işləndi");
        }

        /// <summary>
        /// Köhnə sistem dəstəyi - geriyə uyğunluq üçün - SÖNDÜRÜLÜB
        /// </summary>
        // [Queue("insurance")] // SÖNDÜRÜLÜB - duplikasiya problemi
        public async Task ProcessLegacyInsuranceQueue()
        {
            // Bu metod artıq işləmir - yeni sistem istifadə olunur
            return;
        }
    }
}
