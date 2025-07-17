using System;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SigortaYoxla.Data;
using SigortaYoxla.Jobs;
using SigortaYoxla.Services;

namespace SigortaYoxla
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 SİGORTA YOXLA - HANGFIRE CONSOLE APP + DASHBOARD");
            Console.WriteLine("=".PadRight(55, '='));
            Console.WriteLine($"📅 Başlanğıc: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
            Console.WriteLine();

            // Configuration qurulması
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Connection string alınması
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            Console.WriteLine("🔗 Verilənlər bazası bağlantısı konfiqurasiya edildi");

            // DI konteyner qurulması
            var services = new ServiceCollection();
            
            // DbContext qurulması
            services.AddDbContext<ApplicationDbContext>(options => 
                options.UseSqlServer(connectionString));
                
            // Service Provider yaradılması
            var serviceProvider = services.BuildServiceProvider();
            
            // DbContext əldə edilməsi və migration
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            try
            {
                // Database yaradılması və migrationların tətbiq edilməsi
                dbContext.Database.EnsureCreated();
                Console.WriteLine("✅ Verilənlər bazası hazırdır");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Verilənlər bazası xətası: {ex.Message}");
                Console.WriteLine("ℹ️  LocalDB istifadə edilir. SQL Server Management Studio ilə əlaqə yoxlayın.");
            }
            
            // QueueRepository-nin initialise edilməsi
            QueueRepository.Initialize(dbContext);
            
            // Test məlumatlarını yüklə
            QueueRepository.SeedTestData();

            // Hangfire konfiqurasiyası - SQL Server
            GlobalConfiguration.Configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseColouredConsoleLogProvider()
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
                {
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.FromSeconds(15),
                    UseRecommendedIsolationLevel = true,
                    DisableGlobalLocks = true
                });

            Console.WriteLine("🔧 Hangfire konfiqurasiya edildi (SQL Server)");

            // Web Host yaradılması (Dashboard üçün)
            var host = CreateWebHost(args, connectionString);
            
            // Web Host-u background-da başlat
            var hostTask = host.RunAsync();
            
            Console.WriteLine("🌐 Hangfire Dashboard başladı: http://localhost:5000/hangfire");
            Console.WriteLine("🔗 Dashboard linki: http://localhost:5000/hangfire");
            
            // Hangfire background server
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

            Console.WriteLine("✅ Sistem hazırdır!");
            Console.WriteLine("🌐 Dashboard: http://localhost:5000/hangfire");
            Console.WriteLine("📊 Queue statusunu görmək üçün ENTER basın...");
            Console.WriteLine("❌ Sistemi dayandırmaq üçün ESC basın...");
            Console.WriteLine();

            // Console app-ı canlı saxla
            var cts = new CancellationTokenSource();
            var consoleTask = HandleConsoleInput(cts.Token);
            
            await Task.WhenAny(hostTask, consoleTask);
            
            Console.WriteLine("\n👋 Sistem dayandırılır...");
            cts.Cancel();
        }

        static IHost CreateWebHost(string[] args, string connectionString)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseUrls("http://localhost:5000");
                    webBuilder.Configure(app =>
                    {
                        // Hangfire Dashboard
                        app.UseHangfireDashboard("/hangfire", new DashboardOptions
                        {
                            Authorization = new[] { new AllowAllAuthorizationFilter() }
                        });
                        
                        app.UseRouting();
                    });
                    webBuilder.ConfigureServices(services =>
                    {
                        services.AddDbContext<ApplicationDbContext>(options => 
                            options.UseSqlServer(connectionString));
                        services.AddHangfire(config => config.UseSqlServerStorage(connectionString));
                    });
                })
                .Build();
        }

        static async Task HandleConsoleInput(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
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
                
                await Task.Delay(100, cancellationToken);
            }
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

    /// <summary>
    /// Hangfire Dashboard üçün authorization filter - hamıya icazə verir
    /// </summary>
    public class AllowAllAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            return true; // Hamıya icazə ver
        }
    }
}
