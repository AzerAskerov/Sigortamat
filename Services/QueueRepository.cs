using System;
using System.Collections.Generic;
using System.Linq;
using SigortaYoxla.Models;

namespace SigortaYoxla.Services
{
    /// <summary>
    /// Queue simualtion - həqiqi layihədə database olacaq
    /// </summary>
    public class QueueRepository
    {
        private static List<QueueItem> _queue = new();
        private static int _nextId = 1;

        /// <summary>
        /// Test məlumatları ilə queue-nu doldur
        /// </summary>
        public static void SeedTestData()
        {
            _queue.Clear();
            _nextId = 1;

            // Sığorta yoxlama queue-ları
            var insuranceItems = new[]
            {
                new QueueItem { Id = _nextId++, Type = "insurance", CarNumber = "90HB986", IsProcessed = false },
                new QueueItem { Id = _nextId++, Type = "insurance", CarNumber = "90HB987", IsProcessed = false },
                new QueueItem { Id = _nextId++, Type = "insurance", CarNumber = "90HB988", IsProcessed = false },
            };

            // WhatsApp mesaj queue-ları  
            var whatsappItems = new[]
            {
                new QueueItem { Id = _nextId++, Type = "whatsapp", PhoneNumber = "994555902205", Message = "Salam! Test mesajı 1", IsProcessed = false },
                new QueueItem { Id = _nextId++, Type = "whatsapp", PhoneNumber = "994707877878", Message = "Salam! Test mesajı 2", IsProcessed = false },
                new QueueItem { Id = _nextId++, Type = "whatsapp", PhoneNumber = "994504519279", Message = "Salam! Test mesajı 3", IsProcessed = false },
            };

            _queue.AddRange(insuranceItems);
            _queue.AddRange(whatsappItems);

            Console.WriteLine($"🔄 Queue test məlumatları yükləndi: {_queue.Count} element");
        }

        /// <summary>
        /// Proses olunmamış sığorta queue-larını gətir
        /// </summary>
        public static List<QueueItem> GetUnprocessedInsuranceItems()
        {
            return _queue.Where(q => q.Type == "insurance" && !q.IsProcessed).ToList();
        }

        /// <summary>
        /// Proses olunmamış WhatsApp queue-larını gətir
        /// </summary>
        public static List<QueueItem> GetUnprocessedWhatsAppItems()
        {
            return _queue.Where(q => q.Type == "whatsapp" && !q.IsProcessed).ToList();
        }

        /// <summary>
        /// Queue elementini proses olunmuş kimi işarələ
        /// </summary>
        public static void MarkAsProcessed(int id, string? error = null)
        {
            var item = _queue.FirstOrDefault(q => q.Id == id);
            if (item != null)
            {
                item.IsProcessed = true;
                item.ProcessedAt = DateTime.Now;
                item.Error = error;
            }
        }

        /// <summary>
        /// Bütün queue elementlərinin statusunu göstər
        /// </summary>
        public static void ShowQueueStatus()
        {
            Console.WriteLine("\n📊 QUEUE STATUS:");
            Console.WriteLine("=".PadRight(50, '='));
            
            var total = _queue.Count;
            var processed = _queue.Count(q => q.IsProcessed);
            var pending = total - processed;
            
            Console.WriteLine($"📋 Ümumi: {total}");
            Console.WriteLine($"✅ Proses olunmuş: {processed}");
            Console.WriteLine($"⏳ Gözləyən: {pending}");
            
            if (pending > 0)
            {
                Console.WriteLine("\n⏳ GÖZLƏYƏN QUEUE-LAR:");
                foreach (var item in _queue.Where(q => !q.IsProcessed))
                {
                    Console.WriteLine($"  {item.Id}. {item.Type.ToUpper()}: {item.CarNumber}{item.PhoneNumber}");
                }
            }
        }
    }
}
