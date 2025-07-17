using System;
using System.Threading.Tasks;
using Hangfire;
using SigortaYoxla.Services;

namespace SigortaYoxla.Jobs
{
    /// <summary>
    /// WhatsApp mesaj göndərmə job-u
    /// </summary>
    public class WhatsAppJob
    {
        private readonly WhatsAppService _whatsappService;

        public WhatsAppJob()
        {
            _whatsappService = new WhatsAppService();
        }

        /// <summary>
        /// WhatsApp mesaj göndərmə job-u - hər 2 dəqiqə işləyir
        /// </summary>
        [Queue("whatsapp")]
        public async Task ProcessWhatsAppQueue()
        {
            Console.WriteLine("\n📱 WHATSAPP JOB BAŞLADI");
            Console.WriteLine("=".PadRight(40, '='));
            
            var unprocessedItems = QueueRepository.GetUnprocessedWhatsAppItems();
            
            if (unprocessedItems.Count == 0)
            {
                Console.WriteLine("📋 Proses olunacaq WhatsApp queue-u yoxdur");
                return;
            }

            Console.WriteLine($"📋 {unprocessedItems.Count} WhatsApp queue-u tapıldı");

            foreach (var item in unprocessedItems)
            {
                try
                {
                    Console.WriteLine($"\n🔄 İşlənir: {item.PhoneNumber}");
                    
                    var success = await _whatsappService.SendMessageAsync(item.PhoneNumber, item.Message);
                    
                    if (success)
                    {
                        QueueRepository.MarkAsProcessed(item.Id);
                        Console.WriteLine($"✅ Tamamlandı: {item.PhoneNumber}");
                    }
                    else
                    {
                        QueueRepository.MarkAsProcessed(item.Id, "WhatsApp göndərmə uğursuz");
                        Console.WriteLine($"❌ Uğursuz: {item.PhoneNumber}");
                    }
                    
                    // Rate limiting - WhatsApp üçün daha uzun gözləmə
                    await Task.Delay(3000);
                }
                catch (Exception ex)
                {
                    QueueRepository.MarkAsProcessed(item.Id, ex.Message);
                    Console.WriteLine($"❌ Xəta: {item.PhoneNumber} - {ex.Message}");
                }
            }

            Console.WriteLine($"✅ WhatsApp job tamamlandı: {unprocessedItems.Count} element");
        }
    }
}
