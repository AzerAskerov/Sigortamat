using System.Threading.Tasks;

namespace Sigortamat.TestCases
{
    /// <summary>
    /// Test case interface
    /// </summary>
    public interface ITestCase
    {
        /// <summary>
        /// Test case-in adı
        /// </summary>
        string TestName { get; }

        /// <summary>
        /// Test case-in qısa təsviri
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Test case-i icra edir
        /// </summary>
        Task<TestResult> RunAsync();
    }

    /// <summary>
    /// Test nəticəsi
    /// </summary>
    public class TestResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Details { get; set; }

        public static TestResult CreateSuccess(string? details = null)
        {
            return new TestResult { Success = true, Details = details };
        }

        public static TestResult CreateFailure(string errorMessage, string? details = null)
        {
            return new TestResult { Success = false, ErrorMessage = errorMessage, Details = details };
        }
    }
} 