using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Sigortamat.Data;
using Sigortamat.Models;

namespace Sigortamat.Services
{
    /// <summary>
    /// WhatsApp mesaj işləri üçün repository
    /// </summary>
    public class WhatsAppJobRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly QueueRepository _queueRepository;

        public WhatsAppJobRepository(ApplicationDbContext context, QueueRepository queueRepository)
        {
            _context = context;
            _queueRepository = queueRepository;
        }

        /// <summary>
        /// Yeni WhatsApp mesaj işi yarat
        /// </summary>
        public int CreateWhatsAppJob(string phoneNumber, string messageText, int priority = 0)
        {
            int queueId = _queueRepository.AddToQueue("whatsapp", priority);
            
            var whatsAppJob = new WhatsAppJob
            {
                QueueId = queueId,
                PhoneNumber = phoneNumber,
                MessageText = messageText,
                DeliveryStatus = "pending"
            };
            _context.WhatsAppJobs.Add(whatsAppJob);
            _context.SaveChanges();
            
            Console.WriteLine($"📱 Yeni WhatsApp mesaj işi yaradıldı: {phoneNumber} (Queue ID: {queueId})");
            return queueId;
        }
        
        /// <summary>
        /// WhatsApp mesaj statusunu yenilə
        /// </summary>
        public void UpdateDeliveryStatus(int queueId, string status, string? errorDetails = null, int? processingTimeMs = null)
        {
            var job = _context.WhatsAppJobs.FirstOrDefault(j => j.QueueId == queueId);
            if (job != null)
            {
                job.DeliveryStatus = status;
                job.ErrorDetails = errorDetails;
                job.ProcessingTimeMs = processingTimeMs;
                
                if (status == "sent") job.SentAt = DateTime.Now;
                else if (status == "delivered") job.DeliveredAt = DateTime.Now;
                else if (status == "read") job.ReadAt = DateTime.Now;
                
                _context.SaveChanges();
                
                Console.WriteLine($"📱 WhatsApp mesaj statusu yeniləndi: {job.PhoneNumber} - {status}");
            }
        }
        
        /// <summary>
        /// Gözləyən WhatsApp işlərini gətir
        /// </summary>
        public List<WhatsAppJob> GetPendingWhatsAppJobs(int limit = 10)
        {
            return _context.WhatsAppJobs
                .Join(_context.Queues, 
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
        public WhatsAppJob? GetWhatsAppJobByQueueId(int queueId)
        {
            return _context.WhatsAppJobs
                .Include(j => j.Queue)
                .FirstOrDefault(j => j.QueueId == queueId);
        }
        
        /// <summary>
        /// WhatsApp mesaj statistikası
        /// </summary>
        public void ShowWhatsAppStatistics()
        {
            var total = _context.WhatsAppJobs.Count();
            if (total == 0)
            {
                Console.WriteLine("📊 WhatsApp statistikası: Heç bir məlumat yoxdur");
                return;
            }
            
            var sent = _context.WhatsAppJobs.Count(j => j.DeliveryStatus == "sent");
            var delivered = _context.WhatsAppJobs.Count(j => j.DeliveryStatus == "delivered");
            var read = _context.WhatsAppJobs.Count(j => j.DeliveryStatus == "read");
            var failed = _context.WhatsAppJobs.Count(j => j.DeliveryStatus == "failed");
            
            var avgProcessingTime = _context.WhatsAppJobs
                .Where(j => j.ProcessingTimeMs.HasValue)
                .Select(j => j.ProcessingTimeMs!.Value)
                .DefaultIfEmpty(0)
                .Average();
            
            var avgDeliveryTime = _context.WhatsAppJobs
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
        public void SeedTestData()
        {
            Console.WriteLine("📱 WhatsApp test məlumatları yaradılır...");
            
            CreateWhatsAppJob("994555902205", "Salam! Bu test mesajıdır 1", 0);
            CreateWhatsAppJob("994707877878", "Salam! Bu test mesajıdır 2", 1);
            CreateWhatsAppJob("994504519279", "Salam! Bu test mesajıdır 3", 0);
            
            Console.WriteLine("✅ WhatsApp test məlumatları yaradıldı");
        }
    }
}
