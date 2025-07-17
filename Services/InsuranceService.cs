using System;
using System.Threading.Tasks;
using SigortaYoxla.Models;

namespace SigortaYoxla.Services
{
    /// <summary>
    /// Sığorta yoxlama service - sadə simulation
    /// </summary>
    public class InsuranceService
    {
        /// <summary>
        /// Sığorta yoxla (simulasiya)
        /// </summary>
        public async Task<string> CheckInsuranceAsync(string carNumber)
        {
            try
            {
                Console.WriteLine($"🚗 Sığorta yoxlanır: {carNumber}");
                
                // Simulasiya - real layihədə Selenium olacaq
                await Task.Delay(2000);
                
                var result = $@"
✅ SİGORTA MƏLUMATLARI - {carNumber}
📅 Tarix: {DateTime.Now:dd.MM.yyyy HH:mm}
🏢 Şirkət: Simulate Insurance Co.
📋 Status: Aktiv
⏰ Bitmə tarixi: {DateTime.Now.AddDays(180):dd.MM.yyyy}
💰 Məbləğ: 150 AZN";

                Console.WriteLine($"✅ Sığorta yoxlandı: {carNumber}");
                return result;
            }
            catch (Exception ex)
            {
                var error = $"❌ Sığorta yoxlama xətası ({carNumber}): {ex.Message}";
                Console.WriteLine(error);
                return error;
            }
        }
    }
}
