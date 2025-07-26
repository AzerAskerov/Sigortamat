using Hangfire;
using Hangfire.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sigortamat.Data;
using Sigortamat.Jobs;
using Sigortamat.Services;
using System;
using System.Threading.Tasks;
using Telegram.Bot;

namespace Sigortamat
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            // --- Schedule Recurring Jobs ---
            // We get the service provider to resolve the RecurringJobManager
            using (var scope = host.Services.CreateScope())
            {
                var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
                
                Console.WriteLine("🗓️ Scheduling recurring Hangfire jobs...");

                recurringJobManager.AddOrUpdate<InsuranceJobHandler>(
                    "process-insurance-queue",
                    job => job.ProcessInsuranceQueue(),
                    "*/1 * * * *", // Every minute
                    new RecurringJobOptions { QueueName = "insurance" });

                recurringJobManager.AddOrUpdate<WhatsAppJob>(
                    "process-whatsapp-queue",
                    job => job.ProcessWhatsAppQueue(),
                    "*/2 * * * *", // Every 2 minutes
                    new RecurringJobOptions { QueueName = "whatsapp" });

                recurringJobManager.AddOrUpdate<TelegramBotJob>(
                    "check-telegram-updates",
                    job => job.CheckForUpdatesAsync(),
                    "*/2 * * * * *", // Every 2 seconds
                    new RecurringJobOptions { QueueName = "telegram" });
                
                Console.WriteLine("✅ All recurring jobs scheduled successfully.");
            }

            // --- Run Application ---
            if (args.Length > 0 && args[0].ToLower() == "test")
            {
                Console.WriteLine("🚀 Test mode activated...");
                await SimpleTestRunner.RunTestAsync(args, host.Services);
                return;
            }

            await host.RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    var configuration = hostContext.Configuration;

                    // --- DB Context ---
                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

                    // --- Hangfire ---
                    services.AddHangfire(config => config
                        .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                        .UseSimpleAssemblyNameTypeSerializer()
                        .UseRecommendedSerializerSettings()
                        .UseSqlServerStorage(configuration.GetConnectionString("DefaultConnection")));
                    
                    services.AddHangfireServer(options => {
                        options.Queues = new[] { "default", "insurance", "whatsapp", "telegram" };
                    });

                    // --- Application Services ---
                        services.AddScoped<QueueRepository>();
                        services.AddScoped<InsuranceJobRepository>();
                        services.AddScoped<WhatsAppJobRepository>();
                        services.AddScoped<InsuranceService>();
                    services.AddScoped<WhatsAppService>();
                    services.AddScoped<LeadService>();
                    services.AddScoped<NotificationService>();
                        services.AddScoped<RenewalTrackingService>();

                    // --- Telegram Bot ---
                    services.AddHttpClient("telegram_bot_client")
                        .AddTypedClient<ITelegramBotClient>((httpClient, sp) => 
                            new TelegramBotClient(configuration["Telegram:BotToken"], httpClient));
                    services.AddScoped<TelegramBotService>();
                    
                    // --- Job Handlers ---
                        services.AddScoped<InsuranceJobHandler>();
                        services.AddScoped<WhatsAppJob>();
                    services.AddScoped<TelegramBotJob>();
                });
    }
}
