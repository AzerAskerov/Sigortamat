using System;
using System.Threading.Tasks;
using Sigortamat.Services;

namespace Sigortamat
{
    /// <summary>
    /// Sadəcə DateSetExperiment-i test etmək üçün ayrı runner
    /// Əsas programa toxunmadan təst edə bilərik
    /// </summary>
    class DateExperimentRunner
    {
        static async Task MainExperiment(string[] args)
        {
            Console.WriteLine("🧪 DATE SET EXPERIMENT RUNNER");
            Console.WriteLine("==============================");
            Console.WriteLine($"📅 Start: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
            Console.WriteLine();

            try
            {
                Console.WriteLine("🔬 DateSetExperiment başlayır...");
                
                var result = await DateSetExperiment.TestDateSetting();
                
                Console.WriteLine();
                Console.WriteLine($"🏁 NƏTICƏ: {(result ? "✅ UĞURLU" : "❌ UĞURSUZ")}");
                
                if (result)
                {
                    Console.WriteLine("✨ Tarix set etmə işlədi! InsuranceService-də istifadə edə bilərik.");
                }
                else
                {
                    Console.WriteLine("⚠️ Tarix set etmə işləmədi. Əlavə araşdırma lazımdır.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Xəta: {ex.Message}");
                Console.WriteLine($"📋 Stack Trace: {ex.StackTrace}");
            }
            
            Console.WriteLine();
            Console.WriteLine("✅ Experiment bitdi. ENTER basın...");
            Console.ReadLine();
        }
    }
}
