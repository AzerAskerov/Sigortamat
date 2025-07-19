using System;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Linq;

namespace Sigortamat.Services
{
    /// <summary>
    /// Tarixi set etməyi experiment edən test class
    /// Əsas InsuranceService-ə toxunmadan müxtəlif metodlar test edir
    /// </summary>
    public class DateSetExperiment
    {
        public static async Task<bool> TestDateSetting()
        {
            ChromeDriver? driver = null;
            
            try
            {
                Console.WriteLine("🧪 DATE SET EXPERIMENT BAŞLADI");
                Console.WriteLine("===============================");
                
                var options = new ChromeOptions();
                // Headless=false ki göək nə baş verir
                options.AddArguments(
                    "--no-sandbox",
                    "--disable-dev-shm-usage",
                    "--disable-gpu",
                    "--window-size=1920,1080"
                );

                driver = new ChromeDriver(options);
                driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(60);

                Console.WriteLine("🌐 ISB.az saytına gedirik...");
                await driver.Navigate().GoToUrlAsync("https://services.isb.az/cmtpl/findInsurer");

                await Task.Delay(3000); // Səhifə yüklənsin

                Console.WriteLine("🔍 Səhifədə bütün elementləri analiz edirik...");
                
                // 1. Bütün input elementlərini tap
                var allInputs = driver.FindElements(By.TagName("input"));
                Console.WriteLine($"📊 {allInputs.Count} input elementi tapıldı:");
                
                for (int i = 0; i < allInputs.Count; i++)
                {
                    try
                    {
                        var input = allInputs[i];
                        var name = input.GetAttribute("name") ?? "NO-NAME";
                        var id = input.GetAttribute("id") ?? "NO-ID";
                        var type = input.GetAttribute("type") ?? "NO-TYPE";
                        var placeholder = input.GetAttribute("placeholder") ?? "NO-PLACEHOLDER";
                        var className = input.GetAttribute("class") ?? "NO-CLASS";
                        
                        Console.WriteLine($"   [{i}] Name: {name}, ID: {id}, Type: {type}");
                        Console.WriteLine($"       Placeholder: {placeholder}, Class: {className}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"   [{i}] ERROR: {ex.Message}");
                    }
                }

                Console.WriteLine("\n🗓️ TARIX SET ETMƏ TESTLƏRI:");
                Console.WriteLine("============================");

                var testDate = DateTime.Now;
                var dateStr_ddMMyyyy = $"{testDate:dd/MM/yyyy}";
                var dateStr_yyyyMMdd = $"{testDate:yyyy-MM-dd}";
                var dateStr_ddMMyy = $"{testDate:dd/MM/yy}";
                
                Console.WriteLine($"Test tarixi: {dateStr_ddMMyyyy}");

                // Test 1: Car number input-u tap və doldur (control test)
                bool carNumberWorked = false;
                try
                {
                    var carInput = driver.FindElement(By.Name("carNumber"));
                    carInput.Clear();
                    carInput.SendKeys("10rl096");
                    carNumberWorked = true;
                    Console.WriteLine("✅ Test 1: Car number input işlədi");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Test 1: Car number failed - {ex.Message}");
                }

                if (!carNumberWorked)
                {
                    Console.WriteLine("❌ Car number input tapılmadı, eksperiment dayandırılır");
                    return false;
                }

                // Test 2: Bütün input elementlərində tarixi set etməyə çalış
                Console.WriteLine("\n🔬 Hər input-da tarix set etməyi test edirik:");
                
                var dateSetSuccess = false;
                
                for (int i = 0; i < allInputs.Count; i++)
                {
                    try
                    {
                        var input = allInputs[i];
                        var name = input.GetAttribute("name") ?? "";
                        var type = input.GetAttribute("type") ?? "";
                        var id = input.GetAttribute("id") ?? "";

                        // Tarixi tipini yoxla
                        if (type.ToLower() == "date" || 
                            name.ToLower().Contains("date") || 
                            name.ToLower().Contains("tarix") ||
                            id.ToLower().Contains("date") ||
                            id.ToLower().Contains("tarix"))
                        {
                            Console.WriteLine($"\n🎯 [{i}] Potensial tarix input tapıldı:");
                            Console.WriteLine($"    Name: {name}, Type: {type}, ID: {id}");
                            
                            // Method A: SendKeys
                            try
                            {
                                input.Clear();
                                input.SendKeys(dateStr_ddMMyyyy);
                                Console.WriteLine($"    ✅ Method A (SendKeys dd/MM/yyyy): SUCCESS");
                                dateSetSuccess = true;
                                break;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"    ❌ Method A failed: {ex.Message}");
                            }

                            // Method B: JavaScript value set
                            try
                            {
                                ((IJavaScriptExecutor)driver).ExecuteScript($"arguments[0].value = '{dateStr_ddMMyyyy}';", input);
                                Console.WriteLine($"    ✅ Method B (JS dd/MM/yyyy): SUCCESS");
                                dateSetSuccess = true;
                                break;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"    ❌ Method B failed: {ex.Message}");
                            }

                            // Method C: yyyy-MM-dd format
                            try
                            {
                                input.Clear();
                                ((IJavaScriptExecutor)driver).ExecuteScript($"arguments[0].value = '{dateStr_yyyyMMdd}';", input);
                                Console.WriteLine($"    ✅ Method C (JS yyyy-MM-dd): SUCCESS");
                                dateSetSuccess = true;
                                break;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"    ❌ Method C failed: {ex.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"    ❌ Input [{i}] error: {ex.Message}");
                    }
                }

                // Test 3: Əgər heç bir date input tapılmadısa, ümumi axtarış et
                if (!dateSetSuccess)
                {
                    Console.WriteLine("\n🔍 Ümumi axtarış: bütün input-larda tarix set etməyə çalışaq:");
                    
                    for (int i = 0; i < Math.Min(allInputs.Count, 10); i++) // İlk 10-u test et
                    {
                        try
                        {
                            var input = allInputs[i];
                            var name = input.GetAttribute("name") ?? $"input_{i}";
                            
                            Console.WriteLine($"\n🔬 [{i}] {name} input-unda test:");
                            
                            // JavaScript ilə test et
                            try
                            {
                                ((IJavaScriptExecutor)driver).ExecuteScript($"arguments[0].value = '{dateStr_ddMMyyyy}';", input);
                                
                                // Value set olub olmadığını yoxla
                                var setValue = input.GetAttribute("value");
                                if (!string.IsNullOrEmpty(setValue))
                                {
                                    Console.WriteLine($"    ✅ SUCCESS! Value set: {setValue}");
                                    dateSetSuccess = true;
                                    break;
                                }
                                else
                                {
                                    Console.WriteLine($"    ❌ Value boş qaldı");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"    ❌ JS failed: {ex.Message}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"    ❌ Input [{i}] error: {ex.Message}");
                        }
                    }
                }

                Console.WriteLine($"\n🏁 NƏTICƏ: Tarix set etmə {(dateSetSuccess ? "UĞURLU" : "UĞURSUZ")}");

                // Submit düyməsini tap və bas
                try
                {
                    var submitButton = driver.FindElement(By.Id("pageBody_btnCheck"));
                    submitButton.Click();
                    Console.WriteLine("✅ Submit düyməsi basıldı");
                    
                    await Task.Delay(5000); // Nəticə gözlə
                    
                    // Nəticəni yoxla
                    try
                    {
                        var resultTable = driver.FindElement(By.CssSelector(".result-area table tbody"));
                        Console.WriteLine("✅ Nəticə table tapıldı - sistem işləyir!");
                        return dateSetSuccess;
                    }
                    catch
                    {
                        Console.WriteLine("⚠️ Nəticə table tapılmadı, amma submit işlədi");
                        return dateSetSuccess;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Submit button error: {ex.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Ümumi xəta: {ex.Message}");
                return false;
            }
            finally
            {
                Console.WriteLine("\n🧪 EXPERIMENT TƏBİT");
                driver?.Quit();
            }
        }
    }
}
