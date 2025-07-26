using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sigortamat.Data;
using Sigortamat.Models;
using Sigortamat.Services;

namespace Sigortamat
{
    /// <summary>
    /// Sadə test runner - Lead funksionallığını test edir
    /// </summary>
    public class SimpleTestRunner
    {
        public static async Task RunTestAsync(string[]? args = null, IServiceProvider? serviceProvider = null)
        {
            // If no service provider is passed, create a minimal one for testing
            if (serviceProvider == null)
            {
                var services = new ServiceCollection();
                services.AddLogging(builder => builder.AddConsole());
                
                // DB Context
                var connectionString = "Server=sigortayoxla.database.windows.net;Database=SigortamatDb;User=a.azar1988;Password=54EhP6.G@RKcp8#;TrustServerCertificate=true;";
                services.AddDbContext<ApplicationDbContext>(options => 
                    options.UseSqlServer(connectionString));
                    
                // Services
                services.AddScoped<LeadService>();
                services.AddScoped<NotificationService>();
                services.AddScoped<TelegramBotService>();

                serviceProvider = services.BuildServiceProvider();
            }
            var testManager = new TestCases.TestManager(serviceProvider);

            try
            {
                if (args != null && args.Length > 1)
                {
                    var testName = args[1];
                    
                    if (testName == "help")
                    {
                        testManager.ShowHelp();
                        return;
                    }
                    
                    // Spesifik test icra et
                    Console.WriteLine($"🎯 SPESİFİK TEST: {testName}");
                    Console.WriteLine("=" + new string('=', 30));
                    var result = await testManager.RunTestAsync(testName);
                    
                    if (!result.Success)
                    {
                        Console.WriteLine($"❌ Test uğursuz: {result.ErrorMessage}");
                    }
                }
                else
                {
                    // Bütün testlər
                    await testManager.RunAllTestsAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test manager error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private static async Task CleanupTestDataAsync(ApplicationDbContext context)
        {
            var notifications = await context.Notifications
                .Where(n => n.Lead.CarNumber == "SIMPLE_TEST")
                .ToListAsync();
            context.Notifications.RemoveRange(notifications);

            var leads = await context.Leads
                .Where(l => l.CarNumber == "SIMPLE_TEST")
                .ToListAsync();
            context.Leads.RemoveRange(leads);

            var users = await context.Users
                .Where(u => u.CarNumber == "SIMPLE_TEST")
                .ToListAsync();
            context.Users.RemoveRange(users);

            await context.SaveChangesAsync();
            Console.WriteLine("🗑️ Test data təmizləndi");
        }

        private static async Task ShowDatabaseStateAsync(ApplicationDbContext context)
        {
            var users = await context.Users
                .Where(u => u.CarNumber == "SIMPLE_TEST")
                .ToListAsync();
            
            Console.WriteLine($"👥 Users: {users.Count}");
            foreach (var user in users)
            {
                Console.WriteLine($"   ID: {user.Id}, Car: {user.CarNumber}, Phone: {user.PhoneNumber}");
            }

            var leads = await context.Leads
                .Where(l => l.CarNumber == "SIMPLE_TEST")
                .ToListAsync();
                
            Console.WriteLine($"📋 Leads: {leads.Count}");
            foreach (var lead in leads)
            {
                Console.WriteLine($"   ID: {lead.Id}, Type: {lead.LeadType}, Converted: {lead.IsConverted}");
            }

            var notifications = await context.Notifications
                .Include(n => n.Lead)
                .Where(n => n.Lead.CarNumber == "SIMPLE_TEST")
                .ToListAsync();
                
            Console.WriteLine($"🔔 Notifications: {notifications.Count}");
            foreach (var notification in notifications)
            {
                Console.WriteLine($"   ID: {notification.Id}, Status: {notification.Status}");
                Console.WriteLine($"   Message: {notification.Message.Substring(0, Math.Min(30, notification.Message.Length))}...");
            }
        }
    }
} 