using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sigortamat.Data;
using Sigortamat.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Sigortamat.TestCases
{
    /// <summary>
    /// Base test case - sadə və ümumi funksionallıq
    /// </summary>
    public abstract class BaseTestCase : ITestCase
    {
        protected readonly IServiceProvider _serviceProvider;
        protected readonly ApplicationDbContext _context;
        protected readonly ILogger _logger;

        protected BaseTestCase(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            _logger = serviceProvider.GetRequiredService<ILogger<BaseTestCase>>();
        }

        public abstract string TestName { get; }
        public abstract string Description { get; }

        /// <summary>
        /// Test case-i icra edir və nəticəni qaytarır
        /// </summary>
        public async Task<TestResult> RunAsync()
        {
            Console.WriteLine($"🧪 TEST: {TestName}");
            Console.WriteLine($"📝 {Description}");
            Console.WriteLine("=".PadRight(60, '='));

            try
            {
                // Test-i icra et
                await ExecuteTestAsync();
                
                Console.WriteLine($"✅ Test '{TestName}' uğurla tamamlandı!");
                return TestResult.CreateSuccess($"Test '{TestName}' uğurlu");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test '{TestName}' uğursuz oldu: {ex.Message}");
                _logger.LogError(ex, "Test failed: {TestName}", TestName);
                return TestResult.CreateFailure(ex.Message, ex.StackTrace);
            }
            finally
            {
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Test-in əsas məntiqini icra edir - hər test öz implementasiyasını verir
        /// </summary>
        protected abstract Task ExecuteTestAsync();

        protected async Task<(User User, Lead Lead)> CreateTestDataAsync(string carNumber, string phoneNumber, string leadType)
        {
            // Test data üçün unikal bir sonluq yaradırıq
            var uniqueSuffix = Guid.NewGuid().ToString().Substring(0, 4);
            var testCarNumber = $"{carNumber}_{uniqueSuffix}";

            // User
            var user = new User
            {
                CarNumber = testCarNumber,
                PhoneNumber = phoneNumber,
                NotificationEnabled = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            
            // Lead
            var lead = new Lead
            {
                UserId = user.Id,
                CarNumber = testCarNumber,
                LeadType = leadType,
                Notes = "Automated test lead",
                CreatedAt = DateTime.UtcNow
            };
            _context.Leads.Add(lead);
            await _context.SaveChangesAsync();
            
            return (user, lead);
        }

        protected async Task CleanupOldTestDataAsync(string carNumberPrefix)
        {
            var oldNotifications = await _context.Notifications
                .Where(n => n.Lead.CarNumber.StartsWith(carNumberPrefix + "_"))
                .ToListAsync();
            if(oldNotifications.Any()) _context.Notifications.RemoveRange(oldNotifications);

            var oldLeads = await _context.Leads
                .Where(l => l.CarNumber.StartsWith(carNumberPrefix + "_"))
                .ToListAsync();
            if(oldLeads.Any()) _context.Leads.RemoveRange(oldLeads);

            var oldUsers = await _context.Users
                .Where(u => u.CarNumber.StartsWith(carNumberPrefix + "_"))
                .ToListAsync();
            if(oldUsers.Any()) _context.Users.RemoveRange(oldUsers);
            
            await _context.SaveChangesAsync();
        }

        protected async Task<string> GetUserInputAsync(string prompt)
        {
            Console.Write(prompt);
            return await Task.Run(() => Console.ReadLine() ?? "");
        }
    }
} 