using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Hangfire;
using SigortaYoxla.Services;

namespace SigortaYoxla.Jobs
{
    /// <summary>
    /// WhatsApp mesaj göndərmə job-u - Yeni normallaşdırılmış sistem
    /// </summary>
    public class WhatsAppJob
    {
        private readonly WhatsAppService _whatsappService;

        public WhatsAppJob()
        {
            _whatsappService = new WhatsAppService();
        }

        /// <summary>
        /// Yeni WhatsApp mesaj göndərmə job-u - hər 2 dəqiqə işləyir
        /// </summary>
        [Queue("whatsapp")]
        public async Task ProcessWhatsAppQueue()
        {
            Console.WriteLine("\n📱 WHATSAPP JOB BAŞLADI (Yeni sistem)");
            Console.WriteLine("=".PadRight(50, '='));
            
            var pendingJobs = WhatsAppJobRepository.GetPendingWhatsAppJobs(3);
            
            if (pendingJobs.Count == 0)
            {
                Console.WriteLine("📋 Proses olunacaq WhatsApp işi yoxdur");
                return;
            }

            Console.WriteLine($"📋 {pendingJobs.Count} WhatsApp işi tapıldı");

            foreach (var job in pendingJobs)
            {
                var stopwatch = Stopwatch.StartNew();
                try
                {
                    Console.WriteLine($"\n🔄 İşlənir: {job.PhoneNumber} (Queue ID: {job.QueueId})");
                    Console.WriteLine($"   Mesaj: {job.MessageText.Substring(0, Math.Min(50, job.MessageText.Length))}...");
                    
                    // Queue-u processing kimi işarələ
                    QueueRepository.MarkAsProcessing(job.QueueId);
                    
                    var success = await _whatsappService.SendMessageAsync(job.PhoneNumber, job.MessageText);
                    stopwatch.Stop();
                    
                    if (success)
                    {
                        // WhatsApp job statusunu yenilə
                        WhatsAppJobRepository.UpdateDeliveryStatus(
                            job.QueueId, 
                            "sent", 
                            null,
                            (int)stopwatch.ElapsedMilliseconds
                        );
                        
                        // Queue-u tamamlanmış kimi işarələ
                        QueueRepository.MarkAsCompleted(job.QueueId);
                        Console.WriteLine($"✅ Tamamlandı: {job.PhoneNumber} ({stopwatch.ElapsedMilliseconds}ms)");
                    }
                    else
                    {
                        WhatsAppJobRepository.UpdateDeliveryStatus(
                            job.QueueId, 
                            "failed", 
                            "WhatsApp göndərmə uğursuz",
                            (int)stopwatch.ElapsedMilliseconds
                        );
                        
                        QueueRepository.MarkAsFailed(job.QueueId, "WhatsApp göndərmə uğursuz");
                        Console.WriteLine($"❌ Uğursuz: {job.PhoneNumber}");
                    }
                    
                    // Rate limiting - WhatsApp üçün daha uzun gözləmə
                    await Task.Delay(3000);
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    WhatsAppJobRepository.UpdateDeliveryStatus(
                        job.QueueId, 
                        "failed", 
                        ex.Message,
                        (int)stopwatch.ElapsedMilliseconds
                    );
                    
                    QueueRepository.MarkAsFailed(job.QueueId, ex.Message);
                    Console.WriteLine($"❌ Xəta: {job.PhoneNumber} - {ex.Message}");
                }
            }

            Console.WriteLine($"✅ WhatsApp job tamamlandı: {pendingJobs.Count} element işləndi");
        }
    }
}
