using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sigortamat.Data;
using Sigortamat.Services;

namespace Sigortamat.TestCases
{
    /// <summary>
    /// Bütün test case-lər üçün base sinif
    /// </summary>
    public abstract class TestCaseBase
    {
        protected readonly IServiceProvider _serviceProvider;
        protected readonly ApplicationDbContext _context;
        protected readonly ILogger _logger;

        protected TestCaseBase(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            _logger = serviceProvider.GetRequiredService<ILogger<TestCaseBase>>();
        }

        /// <summary>
        /// Test case-in adı
        /// </summary>
        public abstract string TestName { get; }

        /// <summary>
        /// Test case-in təsviri
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// Test case-i icra edir
        /// </summary>
        public async Task RunAsync()
        {
            Console.WriteLine($"🧪 TEST: {TestName}");
            Console.WriteLine($"📝 {Description}");
            Console.WriteLine("=".PadRight(60, '='));

            try
            {
                // Test data-nı təmizlə
                await CleanupTestDataAsync();
                
                // Test-i icra et
                await ExecuteTestAsync();
                
                // Nəticələri yoxla
                await VerifyResultsAsync();
                
                Console.WriteLine($"✅ Test '{TestName}' uğurla tamamlandı!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test '{TestName}' uğursuz oldu: {ex.Message}");
                _logger.LogError(ex, "Test failed: {TestName}", TestName);
                throw;
            }
            finally
            {
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Test data-nı təmizləyir
        /// </summary>
        protected virtual async Task CleanupTestDataAsync()
        {
            var testCarNumbers = GetTestCarNumbers();
            
            // Test data-nı sil
            var notifications = await _context.Notifications
                .Where(n => testCarNumbers.Contains(n.Lead.CarNumber))
                .ToListAsync();
            _context.Notifications.RemoveRange(notifications);

            var leads = await _context.Leads
                .Where(l => testCarNumbers.Contains(l.CarNumber))
                .ToListAsync();
            _context.Leads.RemoveRange(leads);

            var trackings = await _context.InsuranceRenewalTracking
                .Where(t => testCarNumbers.Contains(t.User.CarNumber))
                .ToListAsync();
            _context.InsuranceRenewalTracking.RemoveRange(trackings);

            var users = await _context.Users
                .Where(u => testCarNumbers.Contains(u.CarNumber))
                .ToListAsync();
            _context.Users.RemoveRange(users);

            await _context.SaveChangesAsync();
            
            Console.WriteLine("🗑️ Test data təmizləndi");
        }

        /// <summary>
        /// Test üçün istifadə edilən avtomobil nömrələri
        /// </summary>
        protected abstract string[] GetTestCarNumbers();

        /// <summary>
        /// Test-in əsas məntiqini icra edir
        /// </summary>
        protected abstract Task ExecuteTestAsync();

        /// <summary>
        /// Test nəticələrini yoxlayır
        /// </summary>
        protected abstract Task VerifyResultsAsync();

        /// <summary>
        /// Database-dən məlumatları yoxlayır və göstərir
        /// </summary>
        protected async Task DisplayDatabaseStateAsync()
        {
            var testCarNumbers = GetTestCarNumbers();

            // Users
            var users = await _context.Users
                .Where(u => testCarNumbers.Contains(u.CarNumber))
                .ToListAsync();
            
            Console.WriteLine($"👥 USERS ({users.Count}):");
            foreach (var user in users)
            {
                Console.WriteLine($"  ID: {user.Id}, Car: {user.CarNumber}, Phone: {user.PhoneNumber ?? "N/A"}");
            }

            // Leads
            var leads = await _context.Leads
                .Include(l => l.User)
                .Where(l => testCarNumbers.Contains(l.CarNumber))
                .ToListAsync();
                
            Console.WriteLine($"📋 LEADS ({leads.Count}):");
            foreach (var lead in leads)
            {
                Console.WriteLine($"  ID: {lead.Id}, Type: {lead.LeadType}, Car: {lead.CarNumber}, Converted: {lead.IsConverted}");
            }

            // Notifications
            var notifications = await _context.Notifications
                .Include(n => n.Lead)
                .Where(n => testCarNumbers.Contains(n.Lead.CarNumber))
                .ToListAsync();
                
            Console.WriteLine($"🔔 NOTIFICATIONS ({notifications.Count}):");
            foreach (var notification in notifications)
            {
                Console.WriteLine($"  ID: {notification.Id}, Status: {notification.Status}, LeadID: {notification.LeadId}");
                Console.WriteLine($"     Message: {notification.Message.Substring(0, Math.Min(50, notification.Message.Length))}...");
            }
        }
    }
} 