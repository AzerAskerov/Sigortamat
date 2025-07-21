using System;
using System.Threading.Tasks;
using Sigortamat.Models;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Diagnostics;
using System.IO;

namespace Sigortamat.Services
{
    /// <summary>
    /// Sığorta yoxlama servisi - ISB.az-dan sığorta məlumatlarını yoxlayır
    /// Real Selenium WebDriver ilə işləyir
    /// </summary>
    public class InsuranceService
    {
        private readonly IConfiguration _configuration;
        private readonly QueueRepository? _queueRepository;

        public InsuranceService(IConfiguration? configuration = null, QueueRepository? queueRepository = null)
        {
            _configuration = configuration ?? GetDefaultConfiguration();
            _queueRepository = queueRepository;
        }

        private static IConfiguration GetDefaultConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true);
            return builder.Build();
        }

        /// <summary>
        /// Sığorta yoxla - InsuranceJob obyekti ilə (4 fərqli hal üçün təkmilləşdirilmiş)
        /// </summary>
        public async Task<InsuranceResult> CheckInsuranceAsync(InsuranceJob job)
        {
            Console.WriteLine($"🔍 Sığorta yoxlanılır: {job.CarNumber} (CheckDate: {job.CheckDate:yyyy-MM-dd})");
            
            var result = await CheckInsuranceWithSeleniumAsync(job);
            
            // Scheduling artıq CheckInsuranceWithSeleniumAsync içində olur
            
            return result;
        }

        /// <summary>
        /// Selenium ilə həqiqi sığorta yoxlama (4 fərqli nəticə üçün optimallaşdırılmış)
        /// </summary>
        private async Task<InsuranceResult> CheckInsuranceWithSeleniumAsync(InsuranceJob job)
        {
            var sw = Stopwatch.StartNew();
            ChromeDriver? driver = null;
            
            try
            {
                var options = new ChromeOptions();
                
                // Configuration-dan headless parametrini oxu
                var useHeadless = _configuration.GetValue<bool>("InsuranceSettings:UseHeadlessBrowser", true);
                
                if (useHeadless)
                {
                    options.AddArgument("--headless");
                }
                
                options.AddArguments(
                    "--no-sandbox",
                    "--disable-dev-shm-usage",
                    "--disable-gpu",
                    "--window-size=1920,1080",
                    "--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"
                );

                driver = new ChromeDriver(options);
                
                // Configuration-dan timeout oxu
                var timeoutSeconds = _configuration.GetValue<int>("InsuranceSettings:TimeoutSeconds", 30);
                driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(timeoutSeconds);

                Console.WriteLine($"🌐 Səhifə açılır: {job.CarNumber}");
                await driver.Navigate().GoToUrlAsync("https://services.isb.az/cmtpl/findInsurer");

                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));

                // Car number input
                var carNumberInput = wait.Until(d => d.FindElement(By.Name("carNumber")));
                carNumberInput.Clear();
                carNumberInput.SendKeys(job.CarNumber.ToLower());

                // Tarixi set et (experiment-dən öyrəndik: name="date" + JavaScript işləyir)
                var dateStr = $"{job.CheckDate:dd/MM/yyyy}";
                Console.WriteLine($"🗓️ Tarixi set etməyə çalışırıq: {dateStr}");
                
                // UĞURLU METOD: name="date" input-u JavaScript ilə
                try
                {
                    var dateInput = driver.FindElement(By.Name("date"));
                    // SendKeys işləmir, JavaScript işləyir
                    ((IJavaScriptExecutor)driver).ExecuteScript($"arguments[0].value = '{dateStr}';", dateInput);
                    Console.WriteLine($"✅ Tarix uğurla set edildi: {dateStr}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Tarix set edilə bilmədi: {ex.Message}");
                    Console.WriteLine($"⚠️ Default tarixlə davam edirik...");
                }

                // Submit button
                var submitButton = wait.Until(d => d.FindElement(By.Id("pageBody_btnCheck")));
                submitButton.Click();

                Console.WriteLine($"✅ Submit düyməsi basıldı, nəticə gözlənilir...");
                
                // Daha uzun gözləmə - sayt yavaş ola bilər
                await Task.Delay(5000);

                // Nəticə məlumatını yoxla - 4 fərqli hal
                try 
                {
                    // 1. Hal: Sığorta tapıldı - real table structure
                    var resultTable = driver.FindElement(By.CssSelector(".result-area table tbody"));
                    var rows = resultTable.FindElements(By.TagName("tr"));

                    foreach (var row in rows)
                    {
                        var cells = row.FindElements(By.TagName("td"));
                        if (cells.Count >= 5)
                        {
                            var company = cells[0].Text.Trim();
                            var plateNumber = cells[1].Text.Trim();
                            var brand = cells[2].Text.Trim();
                            var model = cells[3].Text.Trim();
                            var status = cells[4].Text.Trim();

                            if (plateNumber.Equals(job.CarNumber, StringComparison.OrdinalIgnoreCase))
                            {
                                Console.WriteLine($"✅ Sığorta tapıldı: {company}");
                                return new InsuranceResult
                                {
                                    Success = true,
                                    Company = company,
                                    VehicleBrand = brand,
                                    VehicleModel = model,
                                    DurationMs = (int)sw.ElapsedMilliseconds,
                                    ResultText = "Sığorta məlumatları tapıldı"
                                };
                            }
                        }
                    }
                }
                catch (NoSuchElementException) { /* Normal flow */ }

                // 2. Hal: "Məlumat tapılmadı" mesajı
                try 
                {
                    var noDataElement = driver.FindElement(By.XPath("//span[contains(text(), 'Məlumat tapılmadı')]"));
                    if (noDataElement.Displayed)
                    {
                        Console.WriteLine($"⚠️ Məlumat tapılmadı: {job.CarNumber}");
                        return new InsuranceResult
                        {
                            Success = true, // Still a success, just no data
                            ResultText = "Məlumat tapılmadı"
                        };
                    }
                }
                catch (NoSuchElementException) { /* Normal flow */ }

                // 3. Hal: Gündəlik limit xətası
                try 
                {
                    var limitElement = driver.FindElement(By.XPath("//span[contains(text(), 'Eyni DQN/Müqavilə no. üzrə gün ərzində yalnız 3 sorğu göndərilə bilər')]"));
                    if (limitElement.Displayed)
                    {
                        Console.WriteLine($"🚫 Gündəlik limit doldu: {job.CarNumber}");
                        
                        // Bu job-u sabaha yenidən planlaşdır
                        var tomorrow = DateTime.Today.AddDays(1).AddHours(8); // Sabah saat 08:00
                        Console.WriteLine($"⏰ Queue {job.QueueId} yenidən planlandı: {tomorrow} - Gündəlik limit dolduğu üçün sabah yenidən cəhd");
                        if (_queueRepository != null)
                        {
                            _queueRepository.RescheduleJob(job.QueueId, tomorrow, "Gündəlik limit doldu");
                        }
                        
                        return new InsuranceResult
                        {
                            Success = false,
                            ResultText = "DailyLimitExceeded"
                        };
                    }
                }
                catch (NoSuchElementException) { /* Normal flow */ }

                // 4. Hal: Digər xəta və ya gözlənilməz vəziyyət
                var pageSource = driver.PageSource;
                var errorMessage = "Naməlum xəta - səhifə oxuna bilmədi";
                
                if (pageSource.Contains("error") || pageSource.Contains("xəta"))
                {
                    errorMessage = "Səhifədə xəta mesajı tapıldı";
                }
                
                Console.WriteLine($"❌ Xəta: {job.CarNumber} - {errorMessage}");
                
                // Xəta halında 5 dəqiqə sonra yenidən cəhd et
                var retryAfter = DateTime.Now.AddMinutes(5);
                Console.WriteLine($"⏰ Queue {job.QueueId} yenidən planlandı: {retryAfter:HH:mm} - Xəta səbəbindən 5 dəqiqə sonra yenidən cəhd");
                if (_queueRepository != null)
                {
                    // Queue artıq completed statusunda ola bilər, yoxla
                    var queue = _queueRepository.GetQueueById(job.QueueId);
                    Console.WriteLine($"🔧 DEBUG - Queue {job.QueueId} status check: {queue?.Status ?? "NULL"}");
                    if (queue != null && queue.Status != "completed")
                    {
                        _queueRepository.RescheduleJob(job.QueueId, retryAfter, errorMessage);
                    }
                    else
                    {
                        Console.WriteLine($"🔧 DEBUG - Queue {job.QueueId} artıq completed statusunda, reschedule edilmir");
                    }
                }
                
                return new InsuranceResult
                {
                    Success = false,
                    ResultText = errorMessage
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Selenium xətası: {job.CarNumber} - {ex.Message}");
                return new InsuranceResult
                {
                    Success = false,
                    ResultText = $"System xətası: {ex.Message}"
                };
            }
            finally
            {
                driver?.Quit();
                sw.Stop();
            }
        }

        /// <summary>
        /// Sığorta yoxla - Car Number və Check Date ilə (sadə API)
        /// </summary>
        public async Task<InsuranceResult> CheckInsuranceAsync(string carNumber, DateTime checkDate)
        {
            var job = new InsuranceJob
            {
                CarNumber = carNumber,
                CheckDate = checkDate,
                QueueId = 0 // Manual çağırış üçün
            };
            
            return await CheckInsuranceAsync(job);
        }

        /// <summary>
        /// Chrome sürücüsü test et
        /// </summary>
        public static bool TestChromeDriver()
        {
            try
            {
                var options = new ChromeOptions();
                options.AddArguments("--headless", "--no-sandbox", "--disable-dev-shm-usage");
                
                using var driver = new ChromeDriver(options);
                driver.Navigate().GoToUrl("https://www.google.com");
                
                Console.WriteLine("✅ Chrome sürücüsü işləyir");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Chrome sürücüsü xətası: {ex.Message}");
                return false;
            }
        }
    }
}
