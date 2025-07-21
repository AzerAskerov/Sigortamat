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
using Sigortamat.Data;
using Sigortamat.Jobs;
using Sigortamat.Services;

namespace Sigortamat
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 SİGORTAMAT - AVTOMATLAŞDIRILMIŞ SİGORTA SİSTEMİ");
            Console.WriteLine("=".PadRight(55, '='));
            Console.WriteLine($"📅 Başlanğıc: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
            Console.WriteLine();

            // Configuration qurulması
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Connection string alınması
            var connectionString = configuration.GetConnectionString("DefaultConnection") ?? 
                "Server=(localdb)\\mssqllocaldb;Database=SigortamatDb;Trusted_Connection=true;";
            Console.WriteLine("🔗 Verilənlər bazası bağlantısı konfiqurasiya edildi");

            // DI konteyner qurulması
            var services = new ServiceCollection();
            
            // Logging əlavə et
            services.AddLogging(builder => builder.AddConsole());
            
            // DbContext qurulması
            services.AddDbContext<ApplicationDbContext>(options => 
                options.UseSqlServer(connectionString));
                
            // Job handler servislərini əlavə et
            services.AddScoped<InsuranceJobHandler>();
            services.AddScoped<WhatsAppJob>();
            services.AddScoped<InsuranceService>();
            services.AddScoped<WhatsAppService>();
            services.AddScoped<QueueRepository>();
            services.AddScoped<InsuranceJobRepository>();
            services.AddScoped<WhatsAppJobRepository>();
            services.AddScoped<RenewalTrackingService>();
                
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
            
            Console.WriteLine("✅ Sistem hazırdır - manuel queue əlavə edilməsini gözləyir");

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
            var host = CreateWebHost(args, connectionString, configuration);
            
            // Web Host-u background-da başlat
            var hostTask = host.RunAsync();
            
            Console.WriteLine("🌐 Hangfire Dashboard başladı: http://localhost:5000/hangfire");
            Console.WriteLine("🔗 Dashboard linki: http://localhost:5000/hangfire");
            
            // Hangfire Job Activator set et
            var jobActivator = new CustomJobActivator(serviceProvider);
            GlobalConfiguration.Configuration.UseActivator(jobActivator);
            
            // Hangfire background server
            using var server = new BackgroundJobServer(new BackgroundJobServerOptions
            {
                Queues = new[] { "insurance", "whatsapp", "default" },
                WorkerCount = 1 // 1 işçi thread (iki browser açılmasın)
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
            Console.WriteLine("📈 Sığorta statistikası üçün 'S' basın...");
            Console.WriteLine("📱 WhatsApp statistikası üçün 'W' basın...");
            Console.WriteLine("❌ Sistemi dayandırmaq üçün ESC basın...");
            Console.WriteLine();

            // Console app-ı canlı saxla
            var cts = new CancellationTokenSource();
            var consoleTask = HandleConsoleInput(cts.Token);
            
            await Task.WhenAny(hostTask, consoleTask);
            
            Console.WriteLine("\n👋 Sistem dayandırılır...");
            cts.Cancel();
        }

        static IHost CreateWebHost(string[] args, string connectionString, IConfiguration configuration)
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
                        // Logging əlavə et
                        services.AddLogging(builder => builder.AddConsole());
                        
                        services.AddDbContext<ApplicationDbContext>(options => 
                            options.UseSqlServer(connectionString));
                        
                        // Configuration artıq default olaraq register olub
                        
                        // Repository və servis qeydiyyatları
                        services.AddScoped<QueueRepository>();
                        services.AddScoped<InsuranceJobRepository>();
                        services.AddScoped<WhatsAppJobRepository>();
                        services.AddScoped<InsuranceService>();
                        services.AddScoped<RenewalTrackingService>();
                        services.AddScoped<InsuranceJobHandler>();
                        services.AddScoped<WhatsAppJob>();
                        
                        services.AddHangfire(config => config.UseSqlServerStorage(connectionString));
                        
                        // Hangfire Job Activator konfiqurasiyası
                        services.AddSingleton<JobActivator>(provider => new CustomJobActivator(provider));
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
                    // Queue status göstərmək üçün service provider istifadə et
                    Console.WriteLine("📋 Queue status göstərilir...");
                }
                else if (key.Key == ConsoleKey.S)
                {
                    // Insurance statistics göstərmək üçün service provider istifadə et
                    Console.WriteLine("📊 Insurance statistics göstərilir...");
                }
                else if (key.Key == ConsoleKey.W)
                {
                    // WhatsApp statistics göstərmək üçün service provider istifadə et
                    Console.WriteLine("📱 WhatsApp statistics göstərilir...");
                }
                else if (key.Key == ConsoleKey.D)
                {
                    Console.WriteLine("\n🧪 DATE SET EXPERIMENT başladı...");
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var result = await DateSetExperiment.TestDateSetting();
                            Console.WriteLine($"🧪 EXPERIMENT nəticəsi: {(result ? "UĞURLU" : "UĞURSUZ")}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"🧪 EXPERIMENT xətası: {ex.Message}");
                        }
                    });
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
            // Sadəcə yeni sistem - hər dəqiqə
            RecurringJob.AddOrUpdate<InsuranceJobHandler>(
                "insurance-check",
                job => job.ProcessInsuranceQueue(),
                Cron.Minutely);

            // WhatsApp mesaj job-u - hər 5 dəqiqə  
            RecurringJob.AddOrUpdate<WhatsAppJob>(
                "whatsapp-send",
                job => job.ProcessWhatsAppQueue(),
                "*/5 * * * *"); // Hər 5 dəqiqə
        }

        /// <summary>
        /// Test üçün manual job-lar əlavə et
        /// </summary>
        private static void AddManualTestJobs()
        {
            // İlk dəfə dərhal işləsin
            BackgroundJob.Enqueue<InsuranceJobHandler>(job => job.ProcessInsuranceQueue());
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

    public class CustomJobActivator : JobActivator
    {
        private readonly IServiceProvider _serviceProvider;

        public CustomJobActivator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public override object ActivateJob(Type type)
        {
            return _serviceProvider.GetService(type) ?? Activator.CreateInstance(type);
        }
    }
}
