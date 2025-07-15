using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace SigortaYoxla
{
    public class SigortaChecker
    {
        private ChromeDriver? driver;
        private WebDriverWait? wait;

        public void Initialize(bool enableNetworkLogging = false)
        {
            var options = new ChromeOptions();
            
            // Headless rejimini aktiv et
            options.AddArgument("--headless");
            
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--window-size=1920,1080");
            options.AddArgument("--disable-extensions");
            options.AddArgument("--disable-plugins");
            options.AddArgument("--disable-images");

            // Network logging üçün
            if (enableNetworkLogging)
            {
                options.SetLoggingPreference(LogType.Performance, LogLevel.All);
                options.AddArgument("--enable-network-service-logging");
                options.AddArgument("--log-level=0");
            }

            driver = new ChromeDriver(options);
            wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
        }

        public string CheckInsurance(string carNumber, bool enableNetworkLogging = false)
        {
            try
            {
                if (driver == null || wait == null)
                {
                    return $"❌ {carNumber}: Driver initialize edilməyib";
                }

                Console.WriteLine($"🔍 {carNumber} nömrəsi yoxlanır...");
                
                driver.Navigate().GoToUrl("https://services.isb.az/cmtpl/findInsurer");
                Console.WriteLine("✓ Səhifə yükləndi");

                // Network logları əvvəl yoxla
                if (enableNetworkLogging)
                {
                    // PrintNetworkLogs();
                }

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
                catch (Exception ex)
                {
                    Console.WriteLine($"Tarix input xətası (normal ola bilər): {ex.Message}");
                }

                Thread.Sleep(1000);

                // Yoxla düyməsini tap və bas
                var checkButton = driver.FindElement(By.Id("pageBody_btnCheck"));
                checkButton.Click();
                Console.WriteLine("✓ Düymə basıldı");

                // Network logları yoxla (form göndərildikdən sonra)
                if (enableNetworkLogging)
                {
                    Thread.Sleep(3000); // Network requestlərin tamamlanması üçün gözlə
                    // PrintNetworkLogs();
                }

                // Dinamik gözləmə - result elementi və ya səhifə dəyişikliyi
                Console.WriteLine("⏳ Nəticə gözlənilir...");
                
                // Nəticəni tap - dinamik gözləmə ilə
                try
                {
                    // Çox uzun gözləmə müddəti - 15 saniyə
                    var dynamicWait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));
                    
                    // Result div-ini və ya səhifədə məlumat dəyişikliyini gözlə
                    var resultDiv = dynamicWait.Until(d => 
                    {
                        try 
                        {
                            var result = d.FindElement(By.Id("result"));
                            // Result div tapıldı və məzmunu var
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
                                return body; // Body-də nəticə tapıldı
                            }
                        }
                        catch { }
                        
                        return null; // Hələ hazır deyil
                    });
                    
                    if (resultDiv.TagName.ToLower() == "body")
                    {
                        // Body-dən nəticə alındı
                        var bodyText = resultDiv.Text;
                        if (bodyText.Contains(carNumber.ToUpper()) || bodyText.Contains(carNumber.ToLower()))
                        {
                            return $"✅ {carNumber}: NƏTICƏ TAPILDI\n{ExtractRelevantInfo(bodyText, carNumber)}";
                        }
                        else 
                        {
                            return $"❌ {carNumber}: Heç bir məlumat tapılmadı";
                        }
                    }
                    else
                    {
                        // Result div-dən nəticə alındı
                        var resultText = resultDiv.Text;
                        if (resultText.Contains("Aktiv") || resultText.Contains("tapıldı") || !string.IsNullOrWhiteSpace(resultText))
                        {
                            return $"✅ {carNumber}: SİGORTA TAPILDI\n{resultText}";
                        }
                        else
                        {
                            return $"❌ {carNumber}: Heç bir məlumat tapılmadı";
                        }
                    }
                }
                catch (WebDriverTimeoutException)
                {
                    // Səhifədə başqa elementləri yoxla
                    try 
                    {
                        var bodyText = driver.FindElement(By.TagName("body")).Text;
                        if (bodyText.Contains(carNumber.ToUpper()) || bodyText.Contains(carNumber.ToLower()))
                        {
                            return $"✅ {carNumber}: NƏTICƏ TAPILDI\n{bodyText}";
                        }
                        else 
                        {
                            return $"❌ {carNumber}: Heç bir məlumat tapılmadı";
                        }
                    }
                    catch 
                    {
                        return $"❌ {carNumber}: Heç bir məlumat tapılmadı";
                    }
                }
            }
            catch (Exception ex)
            {
                return $"❌ {carNumber}: Xəta - {ex.Message}";
            }
        }

        private string ExtractRelevantInfo(string bodyText, string carNumber)
        {
            try
            {
                // Nömrəni axtaraq və ətrafında məlumatları tap
                var lines = bodyText.Split('\n');
                var relevantLines = new List<string>();
                
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (line.Contains(carNumber.ToUpper()) || line.Contains(carNumber.ToLower()) ||
                        line.Contains("Aktiv") || line.Contains("Status") || 
                        line.Contains("Təşkilat") || line.Contains("Marka") || line.Contains("Model"))
                    {
                        // Bu xətt və ətrafındakı xətləri əlavə et
                        for (int j = Math.Max(0, i - 2); j <= Math.Min(lines.Length - 1, i + 2); j++)
                        {
                            var contextLine = lines[j].Trim();
                            if (!string.IsNullOrWhiteSpace(contextLine) && 
                                !contextLine.Contains("İcbari Sığorta Bürosu") &&
                                !contextLine.Contains("Bütün hüquqları qorunur") &&
                                !relevantLines.Contains(contextLine))
                            {
                                relevantLines.Add(contextLine);
                            }
                        }
                    }
                }
                
                if (relevantLines.Count > 0)
                {
                    return string.Join("\n", relevantLines.Take(10)); // İlk 10 məlumatı göstər
                }
                
                // Əgər spesifik məlumat tapılmadısa, ümumi axtarış
                foreach (var line in lines)
                {
                    if (line.Contains(carNumber) && line.Length > 10)
                    {
                        return line.Trim();
                    }
                }
                
                return "Məlumat tapıldı, lakin format tanınmadı";
            }
            catch (Exception ex)
            {
                return $"Məlumat çıxarılarkən xəta: {ex.Message}";
            }
        }

        public void Dispose()
        {
            driver?.Quit();
            driver?.Dispose();
        }

        public List<string> CheckInsuranceBulk(List<string> carNumbers, bool enableNetworkLogging = false)
        {
            var results = new List<string>();
            var totalStopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                if (driver == null || wait == null)
                {
                    results.Add("❌ Driver initialize edilməyib");
                    return results;
                }

                Console.WriteLine($"🚗 {carNumbers.Count} nömrə bulk yoxlanır... ⏱️ Vaxt ölçümü başladı\n");
                
                // İlk dəfə səhifəyə get
                driver.Navigate().GoToUrl("https://services.isb.az/cmtpl/findInsurer");
                Console.WriteLine("✓ Səhifə yükləndi (bulk üçün)\n");

                for (int i = 0; i < carNumbers.Count; i++)
                {
                    var carNumber = carNumbers[i];
                    var itemStopwatch = System.Diagnostics.Stopwatch.StartNew();
                    Console.WriteLine($"📋 {i + 1}/{carNumbers.Count}: {carNumber} - ⏱️ {itemStopwatch.Elapsed.TotalSeconds:F1}s");
                    
                    try
                    {
                        // Hər nömrə üçün səhifəni yenilə (ilk dəfədən başqa)
                        if (i > 0)
                        {
                            driver.Navigate().GoToUrl("https://services.isb.az/cmtpl/findInsurer");
                            Console.WriteLine("🔄 Səhifə yeniləndi");
                            Thread.Sleep(1000); // Səhifə yüklənməsi üçün gözlə
                        }
                        // Nömrə input-unu tap və təmizlə
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
                        }
                        catch (Exception) { /* Tarix input olmaya bilər */ }

                        // Kiçik fasilə
                        Thread.Sleep(500);

                        // Yoxla düyməsini tap və bas
                        var checkButton = driver.FindElement(By.Id("pageBody_btnCheck"));
                        checkButton.Click();
                        Console.WriteLine("✓ Düymə basıldı");

                        // Dinamik gözləmə - nəticə üçün
                        Console.WriteLine("⏳ Nəticə gözlənilir...");
                        
                        try
                        {
                            // Qısa gözləmə müddəti - 10 saniyə (bulk üçün daha sürətli)
                            var dynamicWait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                            
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
                            
                            if (resultDiv.TagName.ToLower() == "body")
                            {
                                var bodyText = resultDiv.Text;
                                if (bodyText.Contains(carNumber.ToUpper()) || bodyText.Contains(carNumber.ToLower()))
                                {
                                    itemStopwatch.Stop();
                                    var result = $"✅ {carNumber}: NƏTICƏ TAPILDI (⏱️ {itemStopwatch.Elapsed.TotalSeconds:F1}s)\n{ExtractRelevantInfo(bodyText, carNumber)}";
                                    results.Add(result);
                                    Console.WriteLine($"✅ {carNumber}: Tapıldı - {itemStopwatch.Elapsed.TotalSeconds:F1}s");
                                }
                                else 
                                {
                                    itemStopwatch.Stop();
                                    var result = $"❌ {carNumber}: Heç bir məlumat tapılmadı (⏱️ {itemStopwatch.Elapsed.TotalSeconds:F1}s)";
                                    results.Add(result);
                                    Console.WriteLine($"❌ {carNumber}: Tapılmadı - {itemStopwatch.Elapsed.TotalSeconds:F1}s");
                                }
                            }
                            else
                            {
                                var resultText = resultDiv.Text;
                                if (resultText.Contains("Aktiv") || resultText.Contains("tapıldı") || !string.IsNullOrWhiteSpace(resultText))
                                {
                                    itemStopwatch.Stop();
                                    var result = $"✅ {carNumber}: SİGORTA TAPILDI (⏱️ {itemStopwatch.Elapsed.TotalSeconds:F1}s)\n{resultText}";
                                    results.Add(result);
                                    Console.WriteLine($"✅ {carNumber}: Tapıldı - {itemStopwatch.Elapsed.TotalSeconds:F1}s");
                                }
                                else
                                {
                                    itemStopwatch.Stop();
                                    var result = $"❌ {carNumber}: Heç bir məlumat tapılmadı (⏱️ {itemStopwatch.Elapsed.TotalSeconds:F1}s)";
                                    results.Add(result);
                                    Console.WriteLine($"❌ {carNumber}: Tapılmadı - {itemStopwatch.Elapsed.TotalSeconds:F1}s");
                                }
                            }
                        }
                        catch (WebDriverTimeoutException)
                        {
                            itemStopwatch.Stop();
                            var result = $"❌ {carNumber}: Timeout - məlumat tapılmadı (⏱️ {itemStopwatch.Elapsed.TotalSeconds:F1}s)";
                            results.Add(result);
                            Console.WriteLine($"⏰ {carNumber}: Timeout - {itemStopwatch.Elapsed.TotalSeconds:F1}s");
                        }
                    }
                    catch (Exception ex)
                    {
                        itemStopwatch.Stop();
                        var result = $"❌ {carNumber}: Xəta - {ex.Message} (⏱️ {itemStopwatch.Elapsed.TotalSeconds:F1}s)";
                        results.Add(result);
                        Console.WriteLine($"❌ {carNumber}: Xəta - {itemStopwatch.Elapsed.TotalSeconds:F1}s");
                    }
                    
                    Console.WriteLine(""); // Boş sətir əlavə et
                }
                
                totalStopwatch.Stop();
                Console.WriteLine($"🏁 Bulk yoxlama tamamlandı: {carNumbers.Count} nömrə");
                Console.WriteLine($"⏱️ ÜMUMI VAXT: {totalStopwatch.Elapsed.TotalSeconds:F1} saniyə");
                Console.WriteLine($"📊 ORTA VAXT: {(totalStopwatch.Elapsed.TotalSeconds / carNumbers.Count):F1} saniyə/nömrə");
                return results;
            }
            catch (Exception ex)
            {
                results.Add($"❌ Bulk yoxlama xətası: {ex.Message}");
                return results;
            }
        }
    }
}
