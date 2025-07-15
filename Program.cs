using System;
using System.Collections.Generic;

namespace SigortaYoxla
{
    class Program
    {
        static void Main(string[] args)
        {
            var checker = new SigortaChecker();
            
            try
            {
                var totalStopwatch = System.Diagnostics.Stopwatch.StartNew();
                Console.WriteLine("🚀 BULK Selenium test edəcəyik...\n");
                
                var testCarNumbers = new List<string> { "90HB986", "90HB987", "90HB988" };

                // Headless rejimində bulk test
                Console.WriteLine("=== BULK HEADLESSSelənium - DİNAMİK GÖZLƏMƏLİ ===");
                checker.Initialize(enableNetworkLogging: false);
                var bulkResults = checker.CheckInsuranceBulk(testCarNumbers, enableNetworkLogging: false);
                
                Console.WriteLine("\n🏁 BULK NƏTİCƏLƏR:");
                Console.WriteLine("=".PadRight(50, '='));
                foreach (var result in bulkResults)
                {
                    Console.WriteLine(result);
                    Console.WriteLine("-".PadRight(50, '-'));
                }

                totalStopwatch.Stop();
                Console.WriteLine($"\n✅ Bulk test tamamlandı! {testCarNumbers.Count} nömrə yoxlandı.");
                Console.WriteLine($"🎯 ÜMUMI PROSES VAXTI: {totalStopwatch.Elapsed.TotalSeconds:F1} saniyə");
            }
            finally
            {
                checker.Dispose();
            }
        }
    }
}
