using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Sigortamat.TestCases
{
    /// <summary>
    /// Test manager - spesifik test case-ləri seçib icra edir
    /// </summary>
    public class TestManager
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<string, Func<ITestCase>> _testCases;

        public TestManager(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _testCases = InitializeTestCases();
        }

        /// <summary>
        /// Mövcud test case-ləri əldə edir
        /// </summary>
        public IEnumerable<string> GetAvailableTests()
        {
            return _testCases.Keys;
        }

        /// <summary>
        /// Spesifik test case icra edir
        /// </summary>
        public async Task<TestResult> RunTestAsync(string testName)
        {
            if (!_testCases.ContainsKey(testName))
            {
                Console.WriteLine($"❌ Naməlum test: {testName}");
                Console.WriteLine($"📝 Mövcud testlər: {string.Join(", ", _testCases.Keys)}");
                return TestResult.CreateFailure($"Test tapılmadı: {testName}");
            }

            var testCase = _testCases[testName]();
            return await testCase.RunAsync();
        }

        /// <summary>
        /// Bütün test case-ləri icra edir
        /// </summary>
        public async Task RunAllTestsAsync()
        {
            Console.WriteLine("🚀 BÜTÜN TESTLƏR İCRA EDİLİR");
            Console.WriteLine("=" + new string('=', 40));
            Console.WriteLine($"📅 Başlanğıc: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"📋 Test sayı: {_testCases.Count}");
            Console.WriteLine();

            var results = new List<(string TestName, TestResult Result)>();

            foreach (var (testName, testFactory) in _testCases)
            {
                var testCase = testFactory();
                var result = await testCase.RunAsync();
                results.Add((testName, result));
            }

            // Nəticələri göstər
            Console.WriteLine("📊 TEST NƏTICƏLƏRİ");
            Console.WriteLine("=" + new string('=', 30));

            foreach (var (testName, result) in results)
            {
                var status = result.Success ? "✅ PASS" : "❌ FAIL";
                Console.WriteLine($"{status} {testName}");
                if (!result.Success)
                    Console.WriteLine($"    ❌ {result.ErrorMessage}");
            }

            var passCount = results.Count(r => r.Result.Success);
            var totalCount = results.Count;

            Console.WriteLine();
            Console.WriteLine($"📈 XÜLASƏ: {passCount}/{totalCount} test uğurlu");
            
            if (passCount == totalCount)
            {
                Console.WriteLine("🎉 Bütün testlər uğurla tamamlandı!");
            }
            else
            {
                Console.WriteLine("⚠️ Bəzi testlər uğursuz oldu.");
            }
        }

        /// <summary>
        /// Test case-ləri qeydiyyatdan keçirir
        /// </summary>
        private Dictionary<string, Func<ITestCase>> InitializeTestCases()
        {
            return new Dictionary<string, Func<ITestCase>>
            {
                ["lead-creation"] = () => new LeadCreationTestCase(_serviceProvider),
                ["notification-approval"] = () => new NotificationApprovalTest(_serviceProvider),
                ["interactive"] = () => new InteractiveLeadTestCase(_serviceProvider),
                ["create-lead-only"] = () => new CreateLeadOnlyTestCase(_serviceProvider),
                // Yeni test case-lər burada əlavə edilə bilər
            };
        }

        /// <summary>
        /// Test case əlavə edir
        /// </summary>
        public void RegisterTestCase(string name, Func<ITestCase> testCaseFactory)
        {
            _testCases[name] = testCaseFactory;
        }

        /// <summary>
        /// Help məlumatı göstərir
        /// </summary>
        public void ShowHelp()
        {
            Console.WriteLine("🧪 TEST MANAGER - HELP");
            Console.WriteLine("======================");
            Console.WriteLine();
            Console.WriteLine("📋 Mövcud test case-lər:");
            
            foreach (var testName in _testCases.Keys)
            {
                var testCase = _testCases[testName]();
                Console.WriteLine($"  • {testName}: {testCase.Description}");
            }
            
            Console.WriteLine();
            Console.WriteLine("💡 İstifadə nümunələri:");
            Console.WriteLine("  dotnet run test                    # Bütün testlər");
            Console.WriteLine("  dotnet run test lead-creation      # Spesifik test");
            Console.WriteLine("  dotnet run test help               # Bu kömək");
        }
    }
} 