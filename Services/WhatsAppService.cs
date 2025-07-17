using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace SigortaYoxla.Services
{
    /// <summary>
    /// WhatsApp mesaj göndərmə service
    /// </summary>
    public class WhatsAppService
    {
        private readonly string _whatsappBotPath;
        
        public WhatsAppService()
        {
            _whatsappBotPath = Path.Combine(Directory.GetCurrentDirectory(), "whatsapp-bot");
        }

        /// <summary>
        /// WhatsApp mesajı göndər
        /// </summary>
        public async Task<bool> SendMessageAsync(string phoneNumber, string message)
        {
            try
            {
                Console.WriteLine($"📱 WhatsApp mesajı göndərilir: {phoneNumber}");
                
                var processInfo = new ProcessStartInfo
                {
                    FileName = "node",
                    Arguments = $"debug-whatsapp.js {phoneNumber} \"{message}\"",
                    WorkingDirectory = _whatsappBotPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process == null)
                {
                    Console.WriteLine("❌ Debug prosesi başlamadı");
                    return false;
                }
                
                await process.WaitForExitAsync();
                
                if (process.ExitCode == 0)
                {
                    Console.WriteLine($"✅ WhatsApp mesajı göndərildi: {phoneNumber}");
                    return true;
                }
                else
                {
                    var error = await process.StandardError.ReadToEndAsync();
                    Console.WriteLine($"❌ WhatsApp xətası: {error}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Mesaj göndərmə xətası: {ex.Message}");
                return false;
            }
        }
    }
}
