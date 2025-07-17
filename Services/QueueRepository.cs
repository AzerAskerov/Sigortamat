using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SigortaYoxla.Data;
using SigortaYoxla.Models;

namespace SigortaYoxla.Services
{
    /// <summary>
    /// Queue repository - SQL database ilə işləyir
    /// </summary>
    public class QueueRepository
    {
        private static ApplicationDbContext _dbContext;

        public static void Initialize(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Test məlumatları ilə queue-nu doldur
        /// </summary>
        public static void SeedTestData()
        {
            if (_dbContext.QueueItems.Any())
                return;

            // Sığorta yoxlama queue-ları
            var insuranceItems = new[]
            {
                new QueueItem { Type = "insurance", CarNumber = "90HB986", IsProcessed = false, CreatedAt = DateTime.Now },
                new QueueItem { Type = "insurance", CarNumber = "90HB987", IsProcessed = false, CreatedAt = DateTime.Now },
                new QueueItem { Type = "insurance", CarNumber = "90HB988", IsProcessed = false, CreatedAt = DateTime.Now },
            };

            // WhatsApp mesaj queue-ları
            var whatsappItems = new[]
            {
                new QueueItem { Type = "whatsapp", PhoneNumber = "994555902205", Message = "Salam! Test mesajı 1", IsProcessed = false, CreatedAt = DateTime.Now },
                new QueueItem { Type = "whatsapp", PhoneNumber = "994707877878", Message = "Salam! Test mesajı 2", IsProcessed = false, CreatedAt = DateTime.Now },
                new QueueItem { Type = "whatsapp", PhoneNumber = "994504519279", Message = "Salam! Test mesajı 3", IsProcessed = false, CreatedAt = DateTime.Now },
            };

            _dbContext.QueueItems.AddRange(insuranceItems);
            _dbContext.QueueItems.AddRange(whatsappItems);
            _dbContext.SaveChanges();

            Console.WriteLine($"🔄 Queue test məlumatları yükləndi: {_dbContext.QueueItems.Count()} element");
        }

        /// <summary>
        /// Proses olunmamış sığorta queue-larını gətir
        /// </summary>
        public static List<QueueItem> GetUnprocessedInsuranceItems()
        {
            return _dbContext.QueueItems
                .Where(q => q.Type == "insurance" && !q.IsProcessed)
                .ToList();
        }

        /// <summary>
        /// Proses olunmamış WhatsApp queue-larını gətir
        /// </summary>
        public static List<QueueItem> GetUnprocessedWhatsAppItems()
        {
            return _dbContext.QueueItems
                .Where(q => q.Type == "whatsapp" && !q.IsProcessed)
                .ToList();
        }

        /// <summary>
        /// Queue elementini proses olunmuş kimi işarələ
        /// </summary>
        public static void MarkAsProcessed(int id, string? error = null)
        {
            var item = _dbContext.QueueItems.Find(id);
            if (item != null)
            {
                item.IsProcessed = true;
                item.ProcessedAt = DateTime.Now;
                item.Error = error;
                _dbContext.SaveChanges();
            }
        }

        /// <summary>
        /// Bütün queue elementlərinin statusunu göstər
        /// </summary>
        public static void ShowQueueStatus()
        {
            Console.WriteLine("\n📊 QUEUE STATUS:");
            Console.WriteLine("=".PadRight(50, '='));
            var total = _dbContext.QueueItems.Count();
            var processed = _dbContext.QueueItems.Count(q => q.IsProcessed);
            var pending = total - processed;

            Console.WriteLine($"📋 Ümumi: {total}");
            Console.WriteLine($"✅ Proses olunmuş: {processed}");
            Console.WriteLine($"⏳ Gözləyən: {pending}");

            if (pending > 0)
            {
                Console.WriteLine("\n⏳ GÖZLƏYƏN QUEUE-LAR:");
                var pendingItems = _dbContext.QueueItems.Where(q => !q.IsProcessed).ToList();
                foreach (var item in pendingItems)
                {
                    Console.WriteLine($"  {item.Id}. {item.Type.ToUpper()}: {item.CarNumber}{item.PhoneNumber}");
                }
            }
        }
    }
}
