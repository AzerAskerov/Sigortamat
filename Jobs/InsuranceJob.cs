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
        private readonly IServiceProvider _serviceProvider;

        public InsuranceJobHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            Console.WriteLine($"🔧 Insurance Service rejimi: Real Selenium WebDriver");
        }

        /// <summary>
        /// Yeni sığorta yoxlama job-u - hər dəqiqə işləyir
        /// </summary>
        [Queue("insurance")]
        [DisableConcurrentExecution(timeoutInSeconds: 10 * 60)]
        public async Task ProcessInsuranceQueue()
        {
            Console.WriteLine("\n🚗 SİGORTA JOB BAŞLADI (Yeni sistem)");

            // DI pattern - service locator with null checks
            var queueRepository = (QueueRepository)_serviceProvider.GetService(typeof(QueueRepository));
            var renewalTrackingService = (RenewalTrackingService)_serviceProvider.GetService(typeof(RenewalTrackingService));
            var insuranceJobRepository = (InsuranceJobRepository)_serviceProvider.GetService(typeof(InsuranceJobRepository));
            var insuranceService = (InsuranceService)_serviceProvider.GetService(typeof(InsuranceService));
            
            Console.WriteLine($"🔍 DEBUG: Services - QueueRepo: {queueRepository != null}, RenewalTracking: {renewalTrackingService != null}, JobRepo: {insuranceJobRepository != null}, InsuranceService: {insuranceService != null}");
            
            if (queueRepository == null || renewalTrackingService == null || insuranceJobRepository == null || insuranceService == null)
            {
                Console.WriteLine("❌ Service resolution failed - one or more services are null");
                return;
            }

            int processedCount = 0;
            while (true)
            {
                var queue = queueRepository.DequeueAndMarkAsProcessing("insurance");

                if (queue == null)
                {
                    // No more jobs to process
                    break;
                }
                
                processedCount++;
                var stopwatch = Stopwatch.StartNew();
                try
                {
                    Console.WriteLine($"\n🔄 Queue işlənir: ID {queue.Id} (Type: {queue.Type}, Status: {queue.Status})");

                    var job = await insuranceJobRepository.GetByQueueIdAsync(queue.Id);
                    if (job == null)
                    {
                        Console.WriteLine($"⚠️ Queue {queue.Id} üçün InsuranceJob tapılmadı - data inconsistency");
                        continue;
                    }

                    var result = await insuranceService.CheckInsuranceAsync(job);
                    Console.WriteLine($"🔍 DEBUG: InsuranceService result - Success: {result.Success}, ResultText: {result.ResultText}");

                    // Nəticəni dərhal InsuranceJob cədvəlində yenilə
                    await insuranceJobRepository.UpdateJobResultAsync(job.Id, result, stopwatch.ElapsedMilliseconds);

                    if (result.Success)
                    {
                        Console.WriteLine($"✅ DEBUG: Result.Success = true, ProcessRenewalResultAsync çağırılır - Job ID: {job.Id}");
                        try 
                        {
                            await renewalTrackingService.ProcessRenewalResultAsync(job);
                            queueRepository.MarkAsCompleted(queue.Id);
                            Console.WriteLine($"✅ DEBUG: ProcessRenewalResultAsync tamamlandı və queue completed - Job ID: {job.Id}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ ProcessRenewalResultAsync xətası: {ex.Message}");
                            Console.WriteLine($"❌ StackTrace: {ex.StackTrace}");
                            // ProcessRenewalResultAsync xətası olsa da queue-nu failed etmə
                            // InsuranceService artıq reschedule etmişdir
                        }
                    }
                    else
                    {
                        Console.WriteLine($"❌ DEBUG: Result.Success = false, ProcessRenewalResultAsync çağırılmır - Job ID: {job.Id}");
                    }
                    // Uğursuz olduqda heç bir əməliyyat etmə
                    // InsuranceService artıq RescheduleJob etmişdir
                }
                catch (Exception ex)
                {
                    // Global exception - heç bir queue əməliyyatı etmə
                    // InsuranceService artıq error handling etmişdir
                    Console.WriteLine($"❌ Xəta (Queue {queue.Id}): {ex.Message}");
                }
                finally
                {
                    stopwatch.Stop();
                    Console.WriteLine($"✅ İşləmə tamamlandı: {stopwatch.ElapsedMilliseconds} ms");
                }
            }

            if (processedCount == 0)
            {
                Console.WriteLine("==================================================");
                Console.WriteLine("📋 Proses olunacaq sığorta işi yoxdur");
            }
            else
            {
                Console.WriteLine($"✅ Sığorta job tamamlandı: {processedCount} element işləndi");
            }
        }
    }
}
