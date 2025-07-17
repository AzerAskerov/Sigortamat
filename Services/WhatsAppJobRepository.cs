using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SigortaYoxla.Data;
using SigortaYoxla.Models;

namespace SigortaYoxla.Services
{
    /// <summary>
    /// WhatsApp mesaj işləri üçün repository
    /// </summary>
    public static class WhatsAppJobRepository
    {
        /// <summary>
        /// Yeni WhatsApp mesaj işi yarat
        /// </summary>
        public static int CreateWhatsAppJob(string phoneNumber, string messageText, int priority = 0)
        {
            int queueId = QueueRepository.AddToQueue("whatsapp", priority);
            
            using var db = new ApplicationDbContextFactory().CreateDbContext(new string[0]);
            var whatsAppJob = new WhatsAppJob
            {
                QueueId = queueId,
                PhoneNumber = phoneNumber,
                MessageText = messageText,
                DeliveryStatus = "pending"
            };
            db.WhatsAppJobs.Add(whatsAppJob);
            db.SaveChanges();
            
            Console.WriteLine($"📱 Yeni WhatsApp mesaj işi yaradıldı: {phoneNumber} (Queue ID: {queueId})");
            return queueId;
        }
        
        /// <summary>
        /// WhatsApp mesaj statusunu yenilə
        /// </summary>
        public static void UpdateDeliveryStatus(int queueId, string status, string? errorDetails = null, int? processingTimeMs = null)
        {
            using var db = new ApplicationDbContextFactory().CreateDbContext(new string[0]);
            var job = db.WhatsAppJobs.FirstOrDefault(j => j.QueueId == queueId);
            if (job != null)
            {
                job.DeliveryStatus = status;
                job.ErrorDetails = errorDetails;
                job.ProcessingTimeMs = processingTimeMs;
                
                if (status == "sent") job.SentAt = DateTime.Now;
                else if (status == "delivered") job.DeliveredAt = DateTime.Now;
                else if (status == "read") job.ReadAt = DateTime.Now;
                
                db.SaveChanges();
                
                Console.WriteLine($"📱 WhatsApp mesaj statusu yeniləndi: {job.PhoneNumber} - {status}");
            }
        }
        
        /// <summary>
        /// Gözləyən WhatsApp işlərini gətir
        /// </summary>
        public static List<WhatsAppJob> GetPendingWhatsAppJobs(int limit = 10)
        {
            using var db = new ApplicationDbContextFactory().CreateDbContext(new string[0]);
            return db.WhatsAppJobs
                .Join(db.Queues, 
                    job => job.QueueId, 
                    queue => queue.Id, 
                    (job, queue) => new { Job = job, Queue = queue })
                .Where(x => x.Queue.Status == "pending")
                .OrderBy(x => x.Queue.Priority)
                .ThenBy(x => x.Queue.CreatedAt)
                .Take(limit)
                .Select(x => x.Job)
                .Include(j => j.Queue)
                .ToList();
        }
        
        /// <summary>
        /// WhatsApp işini queue ID ilə gətir
        /// </summary>
        public static WhatsAppJob? GetWhatsAppJobByQueueId(int queueId)
        {
            using var db = new ApplicationDbContextFactory().CreateDbContext(new string[0]);
            return db.WhatsAppJobs
                .Include(j => j.Queue)
                .FirstOrDefault(j => j.QueueId == queueId);
        }
        
        /// <summary>
        /// WhatsApp mesaj statistikası
        /// </summary>
        public static void ShowWhatsAppStatistics()
        {
            using var db = new ApplicationDbContextFactory().CreateDbContext(new string[0]);
            
            var total = db.WhatsAppJobs.Count();
            if (total == 0)
            {
                Console.WriteLine("📊 WhatsApp statistikası: Heç bir məlumat yoxdur");
                return;
            }
            
            var sent = db.WhatsAppJobs.Count(j => j.DeliveryStatus == "sent");
            var delivered = db.WhatsAppJobs.Count(j => j.DeliveryStatus == "delivered");
            var read = db.WhatsAppJobs.Count(j => j.DeliveryStatus == "read");
            var failed = db.WhatsAppJobs.Count(j => j.DeliveryStatus == "failed");
            
            var avgProcessingTime = db.WhatsAppJobs
                .Where(j => j.ProcessingTimeMs.HasValue)
                .Select(j => j.ProcessingTimeMs!.Value)
                .DefaultIfEmpty(0)
                .Average();
            
            var avgDeliveryTime = db.WhatsAppJobs
                .Where(j => j.SentAt.HasValue && j.DeliveredAt.HasValue)
                .Select(j => (j.DeliveredAt!.Value - j.SentAt!.Value).TotalSeconds)
                .DefaultIfEmpty(0)
                .Average();
            
            Console.WriteLine("\n📊 WHATSAPP STATİSTİKASI:");
            Console.WriteLine("=".PadRight(50, '='));
            Console.WriteLine($"📋 Ümumi: {total}");
            Console.WriteLine($"📤 Göndərilmiş: {sent}");
            Console.WriteLine($"✅ Çatdırılmış: {delivered}");
            Console.WriteLine($"👁️ Oxunmuş: {read}");
            Console.WriteLine($"❌ Uğursuz: {failed}");
            Console.WriteLine($"⚡ Orta prosessing vaxtı: {avgProcessingTime:F0}ms");
            if (avgDeliveryTime > 0)
                Console.WriteLine($"📡 Orta çatdırılma vaxtı: {avgDeliveryTime:F1}s");
        }
        
        /// <summary>
        /// Test məlumatları yarat
        /// </summary>
        public static void SeedTestData()
        {
            Console.WriteLine("📱 WhatsApp test məlumatları yaradılır...");
            
            CreateWhatsAppJob("994555902205", "Salam! Bu test mesajıdır 1", 0);
            CreateWhatsAppJob("994707877878", "Salam! Bu test mesajıdır 2", 1);
            CreateWhatsAppJob("994504519279", "Salam! Bu test mesajıdır 3", 0);
            
            Console.WriteLine("✅ WhatsApp test məlumatları yaradıldı");
        }
    }
}
