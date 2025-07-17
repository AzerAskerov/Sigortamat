using System;
using System.Threading.Tasks;
using Hangfire;
using SigortaYoxla.Services;

namespace SigortaYoxla.Jobs
{
    /// <summary>
    /// Sığorta yoxlama job-u
    /// </summary>
    public class InsuranceJob
    {
        private readonly InsuranceService _insuranceService;

        public InsuranceJob()
        {
            _insuranceService = new InsuranceService();
        }

        /// <summary>
        /// Sığorta yoxlama job-u - hər dəqiqə işləyir
        /// </summary>
        [Queue("insurance")]
        public async Task ProcessInsuranceQueue()
        {
            Console.WriteLine("\n🚗 SIGORTA JOB BAŞLADI");
            Console.WriteLine("=".PadRight(40, '='));
            
            var unprocessedItems = QueueRepository.GetUnprocessedInsuranceItems();
            
            if (unprocessedItems.Count == 0)
            {
                Console.WriteLine("📋 Proses olunacaq sığorta queue-u yoxdur");
                return;
            }

            Console.WriteLine($"📋 {unprocessedItems.Count} sığorta queue-u tapıldı");

            foreach (var item in unprocessedItems)
            {
                try
                {
                    Console.WriteLine($"\n🔄 İşlənir: {item.CarNumber}");
                    
                    var result = await _insuranceService.CheckInsuranceAsync(item.CarNumber);
                    
                    QueueRepository.MarkAsProcessed(item.Id);
                    Console.WriteLine($"✅ Tamamlandı: {item.CarNumber}");
                    
                    // Rate limiting
                    await Task.Delay(1000);
                }
                catch (Exception ex)
                {
                    QueueRepository.MarkAsProcessed(item.Id, ex.Message);
                    Console.WriteLine($"❌ Xəta: {item.CarNumber} - {ex.Message}");
                }
            }

            Console.WriteLine($"✅ Sığorta job tamamlandı: {unprocessedItems.Count} element");
        }
    }
}
