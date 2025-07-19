using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;
using Hangfire;
using Sigortamat.Services;
using Sigortamat.Models;
using Microsoft.Extensions.Configuration;

namespace Sigortamat.Jobs
{
    /// <summary>
    /// Sığorta yoxlama job-u - Yeni normallaşdırılmış sistem
    /// </summary>
    public class InsuranceJobHandler
    {
        private readonly InsuranceService _insuranceService;
        private readonly QueueRepository _queueRepository;
        private readonly IConfiguration _configuration;

        public InsuranceJobHandler()
        {
            // Konfiqurasiyani oxu
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            _configuration = builder.Build();
            
            _queueRepository = new QueueRepository();
            _insuranceService = new InsuranceService(_configuration, _queueRepository);
            
            Console.WriteLine($"🔧 Insurance Service rejimi: Real Selenium WebDriver");
        }

        /// <summary>
        /// Yeni sığorta yoxlama job-u - hər dəqiqə işləyir
        /// </summary>
        [Queue("insurance")]
        public async Task ProcessInsuranceQueue()
        {
            Console.WriteLine("\n🚗 SİGORTA JOB BAŞLADI (Yeni sistem)");
            Console.WriteLine("=".PadRight(50, '='));
            
            // ProcessAfter sahəsini nəzərə alan pending jobs-ları gətir
            var pendingQueues = QueueRepository.GetPendingQueues("insurance", 5);
            
            if (pendingQueues.Count == 0)
            {
                Console.WriteLine("📋 Proses olunacaq sığorta işi yoxdur");
                return;
            }

            Console.WriteLine($"📋 {pendingQueues.Count} sığorta queue-u tapıldı");

            foreach (var queue in pendingQueues)
            {
                var stopwatch = Stopwatch.StartNew();
                try
                {
                    Console.WriteLine($"\n🔄 Queue işlənir: ID {queue.Id} (Type: {queue.Type})");
                    
                    // İlk öncə bu queue-ya bağlı InsuranceJob-u tap
                    var insuranceJob = InsuranceJobRepository.GetInsuranceJobByQueueId(queue.Id);
                    if (insuranceJob == null)
                    {
                        Console.WriteLine($"❌ Queue ID {queue.Id} üçün InsuranceJob tapılmadı");
                        QueueRepository.MarkAsFailed(queue.Id, "InsuranceJob tapılmadı");
                        continue;
                    }
                    
                    Console.WriteLine($"🚗 Sığorta yoxlanır: {insuranceJob.CarNumber} (CheckDate: {insuranceJob.CheckDate:dd/MM/yyyy})");
                    
                    // YENİ API istifadə et - InsuranceJob obyekti göndər
                    var result = await _insuranceService.CheckInsuranceAsync(insuranceJob);
                    stopwatch.Stop();
                    
                    // Daily limit halında InsuranceJob-u yeniləməyək, çünki bu, əlaqəli Queue-nun
                    // ProcessAfter sahəsini sıfırlayır.
                    if (result.ResultText != "DailyLimitExceeded" && result.Status != "rescheduled")
                    {
                        // Nəticələri InsuranceJob-a yenilə
                        await UpdateInsuranceJobWithResult(insuranceJob, result, stopwatch.ElapsedMilliseconds);
                    }
                    
                    // Nəticəyə görə Queue status təyin et - MarkAsProcessing çağırmırıq
                    if (result.ResultText == "DailyLimitExceeded" || result.Status == "rescheduled")
                    {
                        // Daily limit - RescheduleJob artıq çağırılıb, Queue "pending" və ProcessAfter set edilib
                        Console.WriteLine($"⏰ Queue ID {queue.Id} sabaha planlaşdırıldı (daily limit)");
                        continue; // Bu queue üçün daha heç nə etməyək
                    }
                    else if (result.Status == "completed")
                    {
                        // Normal tamamlanma - sığorta tapıldı və ya tapılmadı
                        QueueRepository.MarkAsCompleted(queue.Id);
                        
                        if (result.IsValid && !string.IsNullOrEmpty(result.Company))
                        {
                            Console.WriteLine($"✅ {insuranceJob.CarNumber} - Sığorta tapıldı, tamamlandı");
                        }
                        else
                        {
                            Console.WriteLine($"⚠️ {insuranceJob.CarNumber} - Məlumat tapılmadı, amma job tamamlandı");
                        }
                    }
                    else
                    {
                        // Digər xəta halları
                        QueueRepository.MarkAsFailed(queue.Id, result.ErrorMessage ?? "Naməlum xəta");
                        Console.WriteLine($"❌ Queue ID {queue.Id} xəta ilə tamamlandı");
                    }
                    
                    // Rate limiting - sayt arasında gecikmə
                    await Task.Delay(2000);
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    QueueRepository.MarkAsFailed(queue.Id, ex.Message);
                    Console.WriteLine($"❌ Xəta: Queue ID {queue.Id} - {ex.Message}");
                }
            }

            Console.WriteLine($"✅ Sığorta job tamamlandı: {pendingQueues.Count} element işləndi");
        }

        /// <summary>
        /// InsuranceJob-u nəticələrlə yenilə
        /// </summary>
        private async Task UpdateInsuranceJobWithResult(Sigortamat.Models.InsuranceJob job, Sigortamat.Models.InsuranceResult result, long processingTimeMs)
        {
            // Real məlumatları parse et və yenilə
            job.Company = result.Company;
            job.VehicleBrand = result.VehicleBrand;
            job.VehicleModel = result.VehicleModel;
            job.Status = result.Status;
            job.ResultText = result.ResultText;
            job.ProcessingTimeMs = (int)processingTimeMs;
            job.ProcessedAt = DateTime.Now;
            
            // DEBUG: Real məlumatları göstər
            Console.WriteLine($"🔧 DEBUG - Company: {job.Company}, Brand: {job.VehicleBrand}, Model: {job.VehicleModel}");
            
            // Verilənlər bazasına yenilə
            await InsuranceJobRepository.UpdateInsuranceJobAsync(job);
        }
    }
}
