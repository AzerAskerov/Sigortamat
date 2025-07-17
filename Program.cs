using System;
using System.Threading;
using Hangfire;
using Hangfire.InMemory;
using SigortaYoxla.Jobs;
using SigortaYoxla.Services;

namespace SigortaYoxla
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("🚀 SİGORTA YOXLA - HANGFIRE CONSOLE APP");
            Console.WriteLine("=".PadRight(50, '='));
            Console.WriteLine($"📅 Başlanğıc: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
            Console.WriteLine();

            // Queue test məlumatlarını yüklə
            QueueRepository.SeedTestData();

            // Hangfire konfiqurasiyası - InMemory
            GlobalConfiguration.Configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseColouredConsoleLogProvider()
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseInMemoryStorage(); // Production-da SQL Server olacaq

            Console.WriteLine("🔧 Hangfire konfiqurasiya edildi (InMemory)");

            // Hangfire server başlat
            using var server = new BackgroundJobServer(new BackgroundJobServerOptions
            {
                Queues = new[] { "insurance", "whatsapp", "default" },
                WorkerCount = 2 // 2 işçi thread
            });

            Console.WriteLine("🎯 Hangfire Server başladı");
            Console.WriteLine("📋 Queue-lar: insurance, whatsapp");
            Console.WriteLine("👥 Worker sayı: 2");
            Console.WriteLine();

            // Recurring job-ları təyin et
            SetupRecurringJobs();

            Console.WriteLine("⏰ Recurring job-lar təyin edildi");
            Console.WriteLine("🔄 Sığorta job: hər dəqiqə");
            Console.WriteLine("📱 WhatsApp job: hər 2 dəqiqə");
            Console.WriteLine();

            // İlk təst üçün manual job-lar əlavə et
            AddManualTestJobs();

            Console.WriteLine("✅ Sistem hazırdır! CTRL+C ilə dayandırın");
            Console.WriteLine("📊 Queue statusunu görmək üçün ENTER basın...");
            Console.WriteLine();

            // Console app-ı canlı saxla
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                {
                    QueueRepository.ShowQueueStatus();
                }
                else if (key.Key == ConsoleKey.Escape)
                {
                    break;
                }
                
                Thread.Sleep(100);
            }

            Console.WriteLine("\n👋 Sistem dayandırılır...");
        }

        /// <summary>
        /// Recurring job-ları konfiqurasiya et
        /// </summary>
        private static void SetupRecurringJobs()
        {
            // Sığorta yoxlama job-u - hər dəqiqə
            RecurringJob.AddOrUpdate<InsuranceJob>(
                "insurance-check",
                job => job.ProcessInsuranceQueue(),
                Cron.Minutely);

            // WhatsApp mesaj job-u - hər 2 dəqiqə  
            RecurringJob.AddOrUpdate<WhatsAppJob>(
                "whatsapp-send",
                job => job.ProcessWhatsAppQueue(),
                "*/2 * * * *"); // Hər 2 dəqiqə
        }

        /// <summary>
        /// Test üçün manual job-lar əlavə et
        /// </summary>
        private static void AddManualTestJobs()
        {
            // İlk dəfə dərhal işləsin
            BackgroundJob.Enqueue<InsuranceJob>(job => job.ProcessInsuranceQueue());
            BackgroundJob.Schedule<WhatsAppJob>(job => job.ProcessWhatsAppQueue(), TimeSpan.FromSeconds(10));
            
            Console.WriteLine("🧪 Test job-ları əlavə edildi");
        }
    }
}
