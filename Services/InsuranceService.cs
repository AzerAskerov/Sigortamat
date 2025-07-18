using System;
using System.Threading.Tasks;
using Sigortamat.Models;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using System.Linq;

namespace Sigortamat.Services
{
    /// <summary>
    /// Sığorta yoxlama service - Real Selenium-based web scraping
    /// </summary>
    public class InsuranceService
    {
        private readonly bool _useSimulation;
        private readonly IConfiguration _configuration;

        public InsuranceService(bool useSimulation = false)
        {
            _useSimulation = useSimulation;
            
            // Configuration oxu
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            _configuration = builder.Build();
        }

        /// <summary>
        /// Sığorta yoxla (Real Selenium scraping)
        /// </summary>
        public async Task<InsuranceResult> CheckInsuranceAsync(string carNumber)
        {
            if (_useSimulation)
            {
                return await CheckInsuranceSimulationAsync(carNumber);
            }

            try
            {
                Console.WriteLine($"🚗 Real sığorta yoxlanır: {carNumber}");
                
                return await CheckInsuranceWithSeleniumAsync(carNumber);
            }
            catch (Exception ex)
            {
                var error = $"Sığorta yoxlama xətası ({carNumber}): {ex.Message}";
                Console.WriteLine($"❌ {error}");
                return InsuranceResult.Error(error);
            }
        }

        /// <summary>
        /// Real Selenium ilə sığorta yoxlama - İşlək versiya (f122c7c commit-dən)
        /// </summary>
        private async Task<InsuranceResult> CheckInsuranceWithSeleniumAsync(string carNumber)
        {
            IWebDriver? driver = null;
            try
            {
                // Chrome driver options - konfiqurasiyadan oxu
                var options = new ChromeOptions();
                var useHeadless = _configuration.GetValue<bool>("InsuranceSettings:UseHeadlessBrowser");
                
                if (useHeadless)
                {
                    options.AddArgument("--headless"); // Görünməz rejim
                    Console.WriteLine("🔧 Chrome headless rejimində");
                }
                else
                {
                    Console.WriteLine("🔧 Chrome görünən rejimində");
                }
                
                options.AddArgument("--no-sandbox");
                options.AddArgument("--disable-dev-shm-usage");
                options.AddArgument("--disable-gpu");
                options.AddArgument("--window-size=1920,1080");
                options.AddArgument("--disable-extensions");
                options.AddArgument("--disable-plugins");
                options.AddArgument("--disable-images");

                driver = new ChromeDriver(options);
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));

                Console.WriteLine($"🌐 ISB.az saytına giriş edilir: {carNumber}");
                
                // Düzgün URL - işlək versiya
                driver.Navigate().GoToUrl("https://services.isb.az/cmtpl/findInsurer");
                Console.WriteLine("✓ Səhifə yükləndi");
                
                await Task.Delay(1000); // Səhifənin yüklənməsini gözlə

                // Nömrə input-unu tap və daxil et
                var carNumberInput = wait.Until(d => d.FindElement(By.Name("carNumber")));
                carNumberInput.Clear();
                carNumberInput.SendKeys(carNumber.ToLower());
                Console.WriteLine("✓ Nömrə daxil edildi");

                // Tarix input-unu tap və bu günün tarixini daxil et (əgər varsa)
                try
                {
                    var dateInput = driver.FindElement(By.Name("givenDate"));
                    string currentDate = DateTime.Now.ToString("dd/MM/yyyy");
                    ((IJavaScriptExecutor)driver).ExecuteScript($"arguments[0].value = '{currentDate}';", dateInput);
                    Console.WriteLine("✓ Tarix daxil edildi");
                }
                catch (Exception)
                {
                    Console.WriteLine("Tarix input tapılmadı (normal ola bilər)");
                }

                await Task.Delay(1000);

                // Yoxla düyməsini tap və bas
                var checkButton = driver.FindElement(By.Id("pageBody_btnCheck"));
                checkButton.Click();
                Console.WriteLine("✓ Düymə basıldı");

                // Dinamik gözləmə - result elementi və ya səhifə dəyişikliyi
                Console.WriteLine("⏳ Nəticə gözlənilir...");

                try
                {
                    // 15 saniyə gözləmə
                    var dynamicWait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));

                    // Result div-ini və ya səhifədə məlumat dəyişikliyini gözlə
                    var resultDiv = dynamicWait.Until(d =>
                    {
                        try
                        {
                            var result = d.FindElement(By.Id("result"));
                            if (!string.IsNullOrWhiteSpace(result.Text))
                            {
                                return result;
                            }
                        }
                        catch { }

                        // Əgər result div yoxdursa, body-də dəyişiklik axtaraq
                        try
                        {
                            var body = d.FindElement(By.TagName("body"));
                            if (body.Text.Contains(carNumber.ToUpper()) ||
                                body.Text.Contains(carNumber.ToLower()) ||
                                body.Text.Contains("Aktiv") ||
                                body.Text.Contains("tapıldı") ||
                                body.Text.Contains("Status"))
                            {
                                return body;
                            }
                        }
                        catch { }

                        return null;
                    });

                    // Nəticəni parse et və InsuranceResult qaytaraq
                    string resultText = "";
                    if (resultDiv.TagName.ToLower() == "body")
                    {
                        resultText = resultDiv.Text;
                    }
                    else
                    {
                        resultText = resultDiv.Text;
                    }

                    // DEBUG: Real sayt məlumatını konsola çap et
                    Console.WriteLine($"🔍 DEBUG - Saytdan gələn məlumat:");
                    Console.WriteLine($"📄 ResultText uzunluğu: {resultText?.Length}");
                    Console.WriteLine($"📝 TAM MƏLUMAT:");
                    Console.WriteLine("=".PadRight(80, '='));
                    Console.WriteLine(resultText ?? "NULL");
                    Console.WriteLine("=".PadRight(80, '='));

                    // HTML table elementlərini birbaşa parse et
                    var result = ParseInsuranceResultFromHtml(driver, carNumber);
                    Console.WriteLine($"✅ Real sığorta yoxlandı: {carNumber} - {result.Status}");
                    return result;
                }
                catch (WebDriverTimeoutException)
                {
                    // Səhifədə başqa elementləri yoxla
                    try
                    {
                        var bodyText = driver.FindElement(By.TagName("body")).Text;
                        
                        // DEBUG: Timeout sonrası məlumat
                        Console.WriteLine($"🔍 DEBUG (TIMEOUT) - Body məlumatı:");
                        // HTML table elementlərini birbaşa parse et
                        var result = ParseInsuranceResultFromHtml(driver, carNumber);
                        Console.WriteLine($"✅ Real sığorta yoxlandı (HTML parse): {carNumber} - {result.Status}");
                        return result;
                    }
                    catch
                    {
                        Console.WriteLine($"❌ Sığorta məlumatı tapılmadı: {carNumber}");
                        return InsuranceResult.NotFound();
                    }
                }
            }
            catch (NoSuchElementException ex)
            {
                Console.WriteLine($"❌ Sığorta saytında element tapılmadı: {carNumber} - {ex.Message}");
                return InsuranceResult.Error($"Element tapılmadı: {ex.Message}");
            }
            catch (WebDriverTimeoutException ex)
            {
                Console.WriteLine($"❌ Sığorta saytı cavab vermir: {carNumber} - {ex.Message}");
                return InsuranceResult.Error($"Sayt cavab vermir: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ümumi xəta: {carNumber} - {ex.Message}");
                return InsuranceResult.Error($"Ümumi xəta: {ex.Message}");
            }
            finally
            {
                driver?.Quit();
            }
        }

        /// <summary>
        /// HTML table elementlərini birbaşa parse edib InsuranceResult-a çevir
        /// </summary>
        private InsuranceResult ParseInsuranceResultFromHtml(IWebDriver driver, string carNumber)
        {
            try
            {
                Console.WriteLine($"🔍 HTML PARSING: {carNumber} üçün table elementləri axtarılır...");
                
                // Result table-ı tap
                var resultTable = driver.FindElement(By.CssSelector(".result-area table tbody"));
                var rows = resultTable.FindElements(By.TagName("tr"));
                
                Console.WriteLine($"📊 HTML PARSING: {rows.Count} sətir tapıldı");
                
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
                        
                        Console.WriteLine($"📝 HTML PARSING: Sətir - {plateNumber} | {brand} {model} | {status}");
                        
                        if (plateNumber.Equals(carNumber, StringComparison.OrdinalIgnoreCase))
                        {
                            bool isActive = status.Contains("Aktiv") || status.Contains("Active");
                            
                            var result = new InsuranceResult
                            {
                                IsValid = isActive,
                                Status = isActive ? "valid" : "expired",
                                Company = company, // Saytdan gələni olduğu kimi
                                Amount = 150,
                                VehicleBrand = brand,
                                VehicleModel = model,
                                FullResultText = $"{company} {plateNumber} {brand} {model} {status}"
                            };

                            Console.WriteLine($"✅ HTML PARSING SUCCESS: {carNumber}");
                            Console.WriteLine($"   Company: {company}");
                            Console.WriteLine($"   Vehicle: {brand} {model}");
                            Console.WriteLine($"   Status: {status}");
                            return result;
                        }
                    }
                }
                
                Console.WriteLine($"❌ HTML PARSING: {carNumber} table-da tapılmadı");
                return InsuranceResult.NotFound();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ HTML PARSING ERROR: {ex.Message}");
                return InsuranceResult.Error($"HTML parse xətası: {ex.Message}");
            }
        }

        /// <summary>
        /// ISB.az-dan gələn nəticəni parse edib InsuranceResult-a çevir
        /// Real məlumat nümunəsi: "AZƏRBAYCAN SƏNAYE SIĞORTA" AÇIQ SƏHMDAR CƏMİYYƏTİ 10RL092 VAZ 21074 Aktiv
        /// </summary>
        /// <summary>
        /// Simulasiya versiyası (test üçün)
        /// </summary>
        private async Task<InsuranceResult> CheckInsuranceSimulationAsync(string carNumber)
        {
            Console.WriteLine($"🧪 Simulasiya sığorta yoxlanır: {carNumber}");
            
            // Simulasiya - real layihədə Selenium olacaq
            await Task.Delay(2000);
            
            // Test məlumatları - müxtəlif nəticələr üçün
            var result = carNumber.EndsWith("6") || carNumber.EndsWith("7") || carNumber.EndsWith("8")
                ? InsuranceResult.Success(
                    company: "Azerbaijan Insurance Company",
                    ownerName: "Test Müştəri",
                    amount: Random.Shared.Next(100, 300)
                )
                : carNumber.EndsWith("9")
                    ? InsuranceResult.Success(
                        company: "Qala Insurance",
                        ownerName: "Test Müştəri 2",
                        amount: Random.Shared.Next(120, 250)
                    )
                    : InsuranceResult.NotFound();

            Console.WriteLine($"✅ Simulasiya sığorta yoxlandı: {carNumber} - {result.Status}");
            return result;
        }

        /// <summary>
        /// Köhnə metod - geriyə uyğunluq üçün
        /// </summary>
        [Obsolete("Bu metod köhnədir. CheckInsuranceAsync(string) istifadə edin.")]
        public async Task<string> CheckInsuranceAsync(string carNumber, bool legacy)
        {
            var result = await CheckInsuranceAsync(carNumber);
            
            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                return $"❌ {result.ErrorMessage}";
            }

            return $@"
✅ SİGORTA MƏLUMATLARI - {carNumber}
📅 Tarix: {DateTime.Now:dd.MM.yyyy HH:mm}
🏢 Şirkət: {result.Company}
📋 Status: {result.Status}
💰 Məbləğ: {result.Amount} AZN";
        }
    }
}
