using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Sigortamat.TestCases
{
    /// <summary>
    /// Bütün test case-ləri icra edən runner
    /// </summary>
    public class TestRunner
    {
        private readonly IServiceProvider _serviceProvider;

        public TestRunner(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Bütün test case-ləri icra edir
        /// </summary>
        public async Task RunAllTestsAsync()
        {
            Console.WriteLine("🚀 LEAD FUNKSIONALLIĞI TEST RUNNER");
            Console.WriteLine("=" + new string('=', 50));
            Console.WriteLine($"📅 Başlanğıc: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();

            var testCases = GetAllTestCases();
            var results = new List<(string TestName, bool Success, string Error)>();

            foreach (var testCase in testCases)
            {
                try
                {
                    await testCase.RunAsync();
                    results.Add((testCase.TestName, true, ""));
                }
                catch (Exception ex)
                {
                    results.Add((testCase.TestName, false, ex.Message));
                    Console.WriteLine($"❌ Test uğursuz: {ex.Message}");
                    Console.WriteLine();
                }
            }

            // Test nəticələrini göstər
            Console.WriteLine("📊 TEST NƏTICƏLƏRİ");
            Console.WriteLine("=" + new string('=', 50));

            foreach (var (testName, success, error) in results)
            {
                var status = success ? "✅ PASS" : "❌ FAIL";
                Console.WriteLine($"{status} {testName}");
                if (!success)
                    Console.WriteLine($"    Xəta: {error}");
            }

            var passCount = results.Count(r => r.Success);
            var totalCount = results.Count;

            Console.WriteLine();
            Console.WriteLine($"📈 XÜLASƏ: {passCount}/{totalCount} test uğurlu");
            
            if (passCount == totalCount)
            {
                Console.WriteLine("🎉 Bütün testlər uğurla tamamlandı!");
            }
            else
            {
                Console.WriteLine("⚠️ Bəzi testlər uğursuz oldu. Yuxarıda xəta mesajlarını yoxlayın.");
            }
        }

        /// <summary>
        /// Tək test case icra edir
        /// </summary>
        public async Task RunSingleTestAsync<T>() where T : TestCaseBase
        {
            Console.WriteLine("🚀 TƏK TEST İCRASI");
            Console.WriteLine("=" + new string('=', 30));

            try
            {
                var testCase = (T)Activator.CreateInstance(typeof(T), _serviceProvider);
                await testCase.RunAsync();
                Console.WriteLine("🎉 Test uğurla tamamlandı!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test uğursuz: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Mövcud bütün test case-ləri qaytarır
        /// </summary>
        private List<TestCaseBase> GetAllTestCases()
        {
            return new List<TestCaseBase>
            {
                new NoInsuranceLeadTestCase(_serviceProvider),
                new RenewalWindowLeadTestCase(_serviceProvider),
                new NotificationApprovalTestCase(_serviceProvider)
            };
        }
    }
} 