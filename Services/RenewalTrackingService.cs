using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sigortamat.Data;
using Sigortamat.Models;

namespace Sigortamat.Services
{
    /// <summary>
    /// Sığorta yenilənmə tarixi izləmə servisi
    /// 4 fazalı proses: Initial → YearSearch → MonthSearch → FinalCheck → Completed
    /// </summary>
    public class RenewalTrackingService
    {
        private readonly ApplicationDbContext _context;
        private readonly QueueRepository _queueRepository;
        private readonly InsuranceJobRepository _insuranceJobRepository;
        private readonly LeadService _leadService;
        private readonly ILogger<RenewalTrackingService> _logger;

        public RenewalTrackingService(
            ApplicationDbContext context,
            QueueRepository queueRepository,
            InsuranceJobRepository insuranceJobRepository,
            LeadService leadService,
            ILogger<RenewalTrackingService> logger)
        {
            _context = context;
            _queueRepository = queueRepository;
            _insuranceJobRepository = insuranceJobRepository;
            _leadService = leadService;
            _logger = logger;
        }

        /// <summary>
        /// Yeni avtomobil üçün renewal tracking prosesini başladır
        /// </summary>
        public async Task<int> StartRenewalTrackingAsync(string carNumber, string? phoneNumber = null)
        {
            _logger.LogInformation("Starting renewal tracking for car: {CarNumber}", carNumber);

            // İstifadəçi yarat və ya mövcudunu tap
            var user = await _context.Users.FirstOrDefaultAsync(u => u.CarNumber == carNumber);
            if (user == null)
            {
                user = new User
                {
                    CarNumber = carNumber,
                    PhoneNumber = phoneNumber,
                    CreatedAt = DateTime.Now
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created new user for car: {CarNumber}", carNumber);
            }

            // Aktiv izləmə prosesi var mı yoxla
            var existingTracking = await _context.InsuranceRenewalTracking
                .FirstOrDefaultAsync(t => t.UserId == user.Id && 
                                    t.CurrentPhase != "Completed");

            if (existingTracking != null)
            {
                _logger.LogWarning("Active renewal tracking already exists for car: {CarNumber}", carNumber);
                return existingTracking.Id;
            }

            // Yeni izləmə prosesi yarat
            var tracking = new InsuranceRenewalTracking
            {
                UserId = user.Id,
                CurrentPhase = "Initial",
                NextCheckDate = DateTime.Now, // İlk yoxlama indi
                CreatedAt = DateTime.Now
            };

            _context.InsuranceRenewalTracking.Add(tracking);
            await _context.SaveChangesAsync();

            // İlk insurance job yarat
            await CreateInsuranceJobAsync(tracking, carNumber, DateTime.Now);

            _logger.LogInformation("Started renewal tracking with ID: {TrackingId} for car: {CarNumber}", 
                tracking.Id, carNumber);

            return tracking.Id;
        }

        /// <summary>
        /// Tamamlanmış insurance job-u emal edir və növbəti fazaya keçir
        /// </summary>
        public async Task ProcessRenewalResultAsync(InsuranceJob completedJob)
        {
            Console.WriteLine($"🔍 DEBUG: ProcessRenewalResultAsync çağırıldı - Job ID: {completedJob.Id}");
            
            if (completedJob.InsuranceRenewalTrackingId == null)
            {
                Console.WriteLine($"❌ DEBUG: InsuranceRenewalTrackingId NULL - Job ID: {completedJob.Id}");
                return;
            }

            var tracking = await _context.InsuranceRenewalTracking
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == completedJob.InsuranceRenewalTrackingId);

            if (tracking == null)
            {
                Console.WriteLine($"❌ DEBUG: Tracking tapılmadı - Job ID: {completedJob.Id}, TrackingId: {completedJob.InsuranceRenewalTrackingId}");
                _logger.LogWarning("Renewal tracking not found for job: {JobId}", completedJob.Id);
                return;
            }

            Console.WriteLine($"🔍 DEBUG: Processing renewal result - Phase: {tracking.CurrentPhase}, Car: {tracking.User.CarNumber}, Job: {completedJob.Id}");
            _logger.LogInformation("Processing renewal result for phase: {Phase}, car: {CarNumber}", 
                tracking.CurrentPhase, tracking.User.CarNumber);

            try
            {
                switch (tracking.CurrentPhase)
                {
                    case "Initial":
                        await ProcessInitialPhaseAsync(tracking, completedJob);
                        break;

                    case "YearSearch":
                        await ProcessYearSearchPhaseAsync(tracking, completedJob);
                        break;

                    case "MonthSearch":
                        await ProcessMonthSearchPhaseAsync(tracking, completedJob);
                        break;

                    case "FinalCheck":
                        await ProcessFinalCheckPhaseAsync(tracking, completedJob);
                        break;

                    default:
                        _logger.LogWarning("Unknown phase: {Phase} for tracking: {TrackingId}", 
                            tracking.CurrentPhase, tracking.Id);
                        break;
                }

                // Ümumi tracking məlumatlarını yenilə
                tracking.LastCheckDate = completedJob.CheckDate;
                tracking.ChecksPerformed++;
                tracking.LastCheckResult = completedJob.ResultText;
                tracking.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing renewal result for tracking: {TrackingId}", tracking.Id);
                throw;
            }
        }

        #region Phase Processing Methods

        private async Task ProcessInitialPhaseAsync(InsuranceRenewalTracking tracking, InsuranceJob completedJob)
        {
            Console.WriteLine($"=====================================");
            Console.WriteLine($"🔍 DEBUG: ProcessInitialPhaseAsync BAŞLADI");
            Console.WriteLine($"🔍 DEBUG: Job ID: {completedJob.Id}, CheckDate: {completedJob.CheckDate:yyyy-MM-dd}");
            Console.WriteLine($"🔍 DEBUG: Job məlumatları - Company: '{completedJob.Company}', Brand: '{completedJob.VehicleBrand}', Model: '{completedJob.VehicleModel}'");
            Console.WriteLine($"🔍 DEBUG: ResultText: '{completedJob.ResultText}'");
            Console.WriteLine($"=====================================");
            
            // İlk job-un nəticəsini yoxla
            bool hasInsuranceData = !string.IsNullOrWhiteSpace(completedJob.Company) || 
                                   !string.IsNullOrWhiteSpace(completedJob.VehicleBrand) || 
                                   !string.IsNullOrWhiteSpace(completedJob.VehicleModel);
            
            Console.WriteLine($"🔍 DEBUG: hasInsuranceData: {hasInsuranceData}");
            
            if (!hasInsuranceData)
            {
                // Məlumat tapılmadı - avtomobil üçün sığorta yoxdur
                Console.WriteLine($"❌ ❌ ❌ İLK YOXLAMADA MƏLUMAT TAPILMADI - TRACKING BİTİR ❌ ❌ ❌");
                // Lead yaradın: NoInsuranceImmediate
                var lead = new Lead
                {
                    UserId = tracking.UserId,
                    CarNumber = tracking.User.CarNumber,
                    LeadType = "NoInsuranceImmediate",
                    Notes = "İlk sorğuda sığorta məlumatı tapılmadı"
                };
                _context.Leads.Add(lead);
                await _context.SaveChangesAsync();

                // Notification yaradılması
                await _leadService.CreateNotificationForLeadAsync(lead);

                tracking.CurrentPhase = "Completed";
                tracking.NextCheckDate = null; // Daha yoxlama etməyəcək
                
                _logger.LogInformation("No insurance found in initial check, created lead and marked completed for tracking: {TrackingId}", tracking.Id);
                Console.WriteLine($"✅ ✅ ✅ LEAD YARADILDI VƏ TRACKING COMPLETED - NO INSURANCE FOUND ✅ ✅ ✅");
                return;
            }
            
            // Sığorta məlumatları tapıldı - YearSearch fazasına keç
            Console.WriteLine($"✅ ✅ ✅ İLK YOXLAMADA SİGORTA TAPILDI - YEARSEARCH FAZASINA KEÇİR ✅ ✅ ✅");
            tracking.CurrentPhase = "YearSearch";
            tracking.NextCheckDate = completedJob.CheckDate?.AddYears(-1) ?? DateTime.Now.AddYears(-1);

            // Növbəti job yarat
            await CreateInsuranceJobAsync(tracking, tracking.User.CarNumber, tracking.NextCheckDate.Value);

            _logger.LogInformation("Moved to YearSearch phase for tracking: {TrackingId}", tracking.Id);
            Console.WriteLine($"📅 ✅ YEARSEARCH FAZASI BAŞLADI - YENİ JOB YARADILDI ✅ 📅");
        }

        private async Task ProcessYearSearchPhaseAsync(InsuranceRenewalTracking tracking, InsuranceJob completedJob)
        {
            Console.WriteLine($"=====================================");
            Console.WriteLine($"🔍 DEBUG: ProcessYearSearchPhaseAsync BAŞLADI");
            Console.WriteLine($"🔍 DEBUG: Job ID: {completedJob.Id}, CheckDate: {completedJob.CheckDate:yyyy-MM-dd}");
            Console.WriteLine($"🔍 DEBUG: Tracking məlumatları - ID: {tracking.Id}, Phase: {tracking.CurrentPhase}, ChecksPerformed: {tracking.ChecksPerformed}");
            Console.WriteLine($"🔍 DEBUG: Job Company: '{completedJob.Company}', Brand: '{completedJob.VehicleBrand}', Model: '{completedJob.VehicleModel}'");
            Console.WriteLine($"=====================================");
            
            // Əvvəlki job-la müqayisə et
            Console.WriteLine($"🔍 DEBUG: GetPreviousJobAsync çağırılır...");
            var previousJob = await GetPreviousJobAsync(tracking.Id, completedJob.CheckDate);
            
            Console.WriteLine($"🔍 DEBUG: GetPreviousJobAsync nəticəsi - Previous Job: {(previousJob != null ? $"ID {previousJob.Id}, Date: {previousJob.CheckDate:yyyy-MM-dd}" : "NULL")}");
            
            if (previousJob != null)
            {
                Console.WriteLine($"=====================================");
                Console.WriteLine($"🔍 DEBUG: MÜQAYİSƏ BAŞLAYIR:");
                Console.WriteLine($"  Current Job  - Company: '{completedJob.Company}', Brand: '{completedJob.VehicleBrand}', Model: '{completedJob.VehicleModel}'");
                Console.WriteLine($"  Previous Job - Company: '{previousJob.Company}', Brand: '{previousJob.VehicleBrand}', Model: '{previousJob.VehicleModel}'");
                Console.WriteLine($"=====================================");
                
                bool hasChanges = DetectChanges(previousJob, completedJob);
                Console.WriteLine($"🔍 DEBUG: DetectChanges nəticəsi: {hasChanges}");

                if (hasChanges)
                {
                    Console.WriteLine($"=====================================");
                    Console.WriteLine($"✅ ✅ ✅ DƏYİŞİKLİK TAPILDI! ✅ ✅ ✅");
                    Console.WriteLine($"🚀 PHASE CHANGE: YearSearch → MonthSearch");
                    Console.WriteLine($"=====================================");
                    
                    // Dəyişiklik tapıldı - MonthSearch fazasına keç
                    tracking.CurrentPhase = "MonthSearch";
                    
                    // İkili axtarış üçün orta nöqtə hesabla
                    var midDate = CalculateMidDate(completedJob.CheckDate.Value, previousJob.CheckDate.Value);
                    tracking.NextCheckDate = midDate;
                    
                    Console.WriteLine($"🔍 DEBUG: MonthSearch üçün mid date: {midDate:yyyy-MM-dd}");
                    Console.WriteLine($"🔍 DEBUG: Date range: {completedJob.CheckDate:yyyy-MM-dd} <-> {previousJob.CheckDate:yyyy-MM-dd}");

                    await CreateInsuranceJobAsync(tracking, tracking.User.CarNumber, midDate);

                    _logger.LogInformation("Found changes, moved to MonthSearch phase for tracking: {TrackingId}", tracking.Id);
                    Console.WriteLine($"✅ ✅ ✅ MonthSearch fazasına keçid TAMAMLANDI ✅ ✅ ✅");
                    Console.WriteLine($"=====================================");
                    return;
                }
                else
                {
                    Console.WriteLine($"⚠️ ⚠️ ⚠️ Dəyişiklik tapılmadı, YearSearch fazasında davam edirik ⚠️ ⚠️ ⚠️");
                }
            }
            else
            {
                Console.WriteLine($"⚠️ ⚠️ ⚠️ Previous job tapılmadı, YearSearch fazasında davam edirik ⚠️ ⚠️ ⚠️");
            }

            // Dəyişiklik yoxdur - daha əvvələ get
            var nextCheckDate = completedJob.CheckDate?.AddYears(-1) ?? DateTime.Now.AddYears(-2);
            tracking.NextCheckDate = nextCheckDate;
            
            Console.WriteLine($"=====================================");
            Console.WriteLine($"🔍 DEBUG: Növbəti yoxlama tarixi: {nextCheckDate:yyyy-MM-dd}");
            Console.WriteLine($"📅 YEAR SEARCH DAVAM EDİR - yeni job yaradılır");
            Console.WriteLine($"=====================================");
            
            await CreateInsuranceJobAsync(tracking, tracking.User.CarNumber, nextCheckDate);

            _logger.LogInformation("No changes found, continuing YearSearch for tracking: {TrackingId}", tracking.Id);
            Console.WriteLine($"⚠️ DEBUG: YearSearch fazasında qalma TAMAMLANDI");
        }

        private async Task ProcessMonthSearchPhaseAsync(InsuranceRenewalTracking tracking, InsuranceJob completedJob)
        {
            Console.WriteLine($"=====================================");
            Console.WriteLine($"🔍 DEBUG: ProcessMonthSearchPhaseAsync BAŞLADI");
            Console.WriteLine($"🔍 DEBUG: Job ID: {completedJob.Id}, CheckDate: {completedJob.CheckDate:yyyy-MM-dd}");
            Console.WriteLine($"🔍 DEBUG: Job Company: '{completedJob.Company}', Brand: '{completedJob.VehicleBrand}', Model: '{completedJob.VehicleModel}'");
            Console.WriteLine($"=====================================");
            
            // Bütün əlaqəli job-ları əldə et və sırala
            var allJobs = await GetAllRelatedJobsAsync(tracking.Id);
            Console.WriteLine($"🔍 DEBUG: Tracking {tracking.Id} üçün ümumi job sayı: {allJobs.Count}");
            
            foreach (var job in allJobs.OrderBy(j => j.CheckDate))
            {
                bool hasInsurance = !string.IsNullOrWhiteSpace(job.Company);
                Console.WriteLine($"  - Job {job.Id}: {job.CheckDate:yyyy-MM-dd} → Company: '{job.Company}' → Sığorta: {(hasInsurance ? "VAR" : "YOXDUR")}");
            }
            
            // Current job-un sığorta vəziyyətini təyin et
            bool currentHasInsurance = !string.IsNullOrWhiteSpace(completedJob.Company);
            Console.WriteLine($"🔍 DEBUG: Current job sığorta vəziyyəti: {(currentHasInsurance ? "VAR" : "YOXDUR")}");
            
            // MonthSearch üçün intelligent binary search - VAR/YOX və ya COMPANY-based
            InsuranceJob? oppositeJob = null;
            
            // Strategy 1: Klassik VAR/YOX axtarışı
            if (currentHasInsurance)
            {
                oppositeJob = allJobs
                    .Where(j => j.CheckDate < completedJob.CheckDate && string.IsNullOrWhiteSpace(j.Company))
                    .OrderByDescending(j => j.CheckDate)
                    .FirstOrDefault();
                Console.WriteLine($"🔍 DEBUG: Strategy 1 - Sığorta VAR, əvvəldə sığorta OLMAYAN job axtarıram...");
            }
            else
            {
                oppositeJob = allJobs
                    .Where(j => j.CheckDate > completedJob.CheckDate && !string.IsNullOrWhiteSpace(j.Company))
                    .OrderBy(j => j.CheckDate)
                    .FirstOrDefault();
                Console.WriteLine($"🔍 DEBUG: Strategy 1 - Sığorta YOXDUR, sonrada sığorta OLAN job axtarıram...");
            }
            
            // Strategy 2: Əgər VAR/YOX tapılmadısa, COMPANY-based axtarış
            if (oppositeJob == null && currentHasInsurance)
            {
                Console.WriteLine($"🔄 DEBUG: Strategy 1 uğursuz! Strategy 2 - COMPANY-based axtarış başlayır...");
                Console.WriteLine($"🔍 DEBUG: Current company: '{completedJob.Company}'");
                
                // Fərqli şirkət olan job axtarım - current-dən əvvəl və sonra
                var differentCompanyJobs = allJobs
                    .Where(j => !string.IsNullOrWhiteSpace(j.Company) && 
                               !string.IsNullOrWhiteSpace(completedJob.Company) &&
                               !j.Company.Equals(completedJob.Company, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                    
                Console.WriteLine($"🔍 DEBUG: Fərqli şirkət olan job sayı: {differentCompanyJobs.Count}");
                foreach (var diffJob in differentCompanyJobs)
                {
                    Console.WriteLine($"  - Job {diffJob.Id}: {diffJob.CheckDate:yyyy-MM-dd} → '{diffJob.Company}'");
                }
                
                if (differentCompanyJobs.Any())
                {
                    // Current-dən ən yaxın olan fərqli şirkəti tap
                    oppositeJob = differentCompanyJobs
                        .OrderBy(j => Math.Abs((j.CheckDate!.Value - completedJob.CheckDate!.Value).TotalDays))
                        .FirstOrDefault();
                        
                    Console.WriteLine($"✅ DEBUG: Strategy 2 - Fərqli şirkət tapıldı: Job {oppositeJob.Id} → '{oppositeJob.Company}'");
                }
            }
            
            if (oppositeJob == null)
            {
                Console.WriteLine($"❌ ❌ ❌ HƏR İKİ STRATEGY UĞURSUZ - OPPOSITE JOB TAPILMADI ❌ ❌ ❌");
                _logger.LogWarning("No opposite job found for MonthSearch binary search (both VAR/YOX and COMPANY strategies failed), tracking: {TrackingId}", tracking.Id);
                return;
            }
            
            Console.WriteLine($"🔍 DEBUG: Opposite job tapıldı - Job {oppositeJob.Id}: {oppositeJob.CheckDate:yyyy-MM-dd} → Company: '{oppositeJob.Company}'");
            
            // Interval hesabla
            var earlierDate = completedJob.CheckDate < oppositeJob.CheckDate ? completedJob.CheckDate.Value : oppositeJob.CheckDate.Value;
            var laterDate = completedJob.CheckDate > oppositeJob.CheckDate ? completedJob.CheckDate.Value : oppositeJob.CheckDate.Value;
            
            Console.WriteLine($"🔍 DEBUG: Binary search interval: {earlierDate:yyyy-MM-dd} ↔ {laterDate:yyyy-MM-dd}");
            
            var dateDiff = laterDate - earlierDate;
            Console.WriteLine($"🔍 DEBUG: Interval uzunluğu: {dateDiff.TotalDays:F0} gün");

            // 14 gündən az interval kifayət edir (≈2 həftə)
            if (dateDiff.TotalDays <= 14)
            {
                // 2 həftədən az fərq - FinalCheck fazasına keç
                Console.WriteLine($"✅ ✅ ✅ INTERVAL 14 GÜNDƏN AZDIR - FINALCHECK FAZASINA KEÇİR ✅ ✅ ✅");
                tracking.CurrentPhase = "FinalCheck";
                await UpdateUserWithEstimatedDateAsync(tracking.UserId, completedJob, oppositeJob);

                _logger.LogInformation("Interval narrowed to 1 month, moved to FinalCheck phase for tracking: {TrackingId}", tracking.Id);
                Console.WriteLine($"📅 ✅ FINALCHECK FAZASI BAŞLADI ✅ 📅");
                return;
            }

            // İkili axtarışa davam et - interval ortasını hesabla
            var nextDate = CalculateMidDate(earlierDate, laterDate);
            Console.WriteLine($"🔍 DEBUG: Binary search mid-point hesablandı: {nextDate:yyyy-MM-dd}");
            Console.WriteLine($"🔍 DEBUG: Next job tarixi: {nextDate:yyyy-MM-dd}");
            
            tracking.NextCheckDate = nextDate;
            await CreateInsuranceJobAsync(tracking, tracking.User.CarNumber, nextDate);

            _logger.LogInformation("Continuing binary search in MonthSearch phase for tracking: {TrackingId}, next date: {NextDate:yyyy-MM-dd}", tracking.Id, nextDate);
            Console.WriteLine($"📅 ✅ BINARY SEARCH DAVAM EDİR - YENİ JOB YARADILDI ✅ 📅");
            Console.WriteLine($"=====================================");
        }

        private async Task ProcessFinalCheckPhaseAsync(InsuranceRenewalTracking tracking, InsuranceJob completedJob)
        {
            // Prosesi tamamlandı kimi qeyd et
            tracking.CurrentPhase = "Completed";
            tracking.LastCheckResult = $"Prosess tamamlandı. Təxmini tarix: {tracking.User.EstimatedRenewalDay}/{tracking.User.EstimatedRenewalMonth}";

            _logger.LogInformation("Completed renewal tracking for tracking: {TrackingId}, estimated date: {Day}/{Month}", 
                tracking.Id, tracking.User.EstimatedRenewalDay, tracking.User.EstimatedRenewalMonth);

            // TODO: Bildiriş sistemini çağır (Step 5)
            // await _notificationService.ScheduleNotificationsAsync(tracking.User);
        }

        #endregion

        #region Helper Methods

        private async Task CreateInsuranceJobAsync(InsuranceRenewalTracking tracking, string carNumber, DateTime checkDate)
        {
            // Gündəlik limit yoxlaması
            var todayJobsCount = await _context.InsuranceJobs
                .Where(j => j.CarNumber == carNumber && 
                           j.CheckDate.HasValue && 
                           j.CheckDate.Value.Date == DateTime.Today)
                .CountAsync();

            DateTime processAfter = DateTime.Now;
            if (todayJobsCount >= 3)
            {
                // Növbəti günə təxirə sal
                processAfter = DateTime.Today.AddDays(1).AddHours(8);
                _logger.LogInformation("Daily limit reached for car: {CarNumber}, scheduling for tomorrow", carNumber);
            }

            await _insuranceJobRepository.CreateInsuranceJobAsync(
                carNumber: carNumber,
                checkDate: checkDate,
                renewalTrackingId: tracking.Id,
                processAfter: processAfter
            );
        }

        private async Task<InsuranceJob?> GetPreviousJobAsync(int trackingId, DateTime? currentCheckDate)
        {
            Console.WriteLine($"🔍 DEBUG: GetPreviousJobAsync başladı - trackingId: {trackingId}, currentCheckDate: {currentCheckDate}");
            
            // Əvvəlcə bütün job-ları göstər
            var allJobs = await _context.InsuranceJobs
                .Where(j => j.InsuranceRenewalTrackingId == trackingId)
                .OrderByDescending(j => j.CheckDate)
                .ToListAsync();
            
            Console.WriteLine($"🔍 DEBUG: Tracking {trackingId} üçün ümumi job sayı: {allJobs.Count}");
            foreach (var job in allJobs)
            {
                Console.WriteLine($"  - Job {job.Id}: Status={job.Status}, CheckDate={job.CheckDate}, Company={job.Company}");
            }
            
            // Proses ardıcıllığında əvvəlki job-u tap (CheckDate > currentCheckDate olan ən yaxın completed job)
            var previousJob = await _context.InsuranceJobs
                .Where(j => j.InsuranceRenewalTrackingId == trackingId && 
                           j.CheckDate > currentCheckDate &&
                           j.Status == "completed")
                .OrderBy(j => j.CheckDate) // Ascending - ən yaxın sonrakı tarixi tap
                .FirstOrDefaultAsync();
                
            Console.WriteLine($"🔍 DEBUG: GetPreviousJobAsync düzəldilmiş filter result - Previous Job: {(previousJob != null ? $"ID {previousJob.Id}, Status: {previousJob.Status}, Date: {previousJob.CheckDate}" : "NULL")}");
            
            return previousJob;
        }

        private async Task<List<InsuranceJob>> GetAllRelatedJobsAsync(int trackingId)
        {
            return await _context.InsuranceJobs
                .Where(j => j.InsuranceRenewalTrackingId == trackingId && j.Status == "completed")
                .OrderBy(j => j.CheckDate)
                .ToListAsync();
        }

        private bool DetectChanges(InsuranceJob job1, InsuranceJob job2)
        {
            Console.WriteLine($"🔍🔍🔍 DEBUG: DetectChanges BAŞLAYIR 🔍🔍🔍");
            Console.WriteLine($"  Job1 (Previous): Company='{job1.Company}', Brand='{job1.VehicleBrand}', Model='{job1.VehicleModel}'");
            Console.WriteLine($"  Job2 (Current):  Company='{job2.Company}', Brand='{job2.VehicleBrand}', Model='{job2.VehicleModel}'");
            
            bool companyChange = job1.Company != job2.Company;
            bool brandChange = job1.VehicleBrand != job2.VehicleBrand;
            bool modelChange = job1.VehicleModel != job2.VehicleModel;
            bool nullChange = string.IsNullOrEmpty(job1.Company) != string.IsNullOrEmpty(job2.Company);
            
            Console.WriteLine($"  📊 Company change: {companyChange} ('{job1.Company}' vs '{job2.Company}')");
            Console.WriteLine($"  📊 Brand change:   {brandChange} ('{job1.VehicleBrand}' vs '{job2.VehicleBrand}')"); 
            Console.WriteLine($"  📊 Model change:   {modelChange} ('{job1.VehicleModel}' vs '{job2.VehicleModel}')");
            Console.WriteLine($"  📊 Null change:    {nullChange} (IsEmpty: '{string.IsNullOrEmpty(job1.Company)}' vs '{string.IsNullOrEmpty(job2.Company)}')");
            
            bool hasChanges = nullChange || companyChange || brandChange || modelChange;
            
            if (hasChanges)
            {
                Console.WriteLine($"✅✅✅ DetectChanges RESULT: {hasChanges} - DƏYİŞİKLİK VAR! ✅✅✅");
                if (companyChange) Console.WriteLine($"    👉 Şirkət dəyişdi: {job1.Company} → {job2.Company}");
                if (brandChange) Console.WriteLine($"    👉 Marka dəyişdi: {job1.VehicleBrand} → {job2.VehicleBrand}");
                if (modelChange) Console.WriteLine($"    👉 Model dəyişdi: {job1.VehicleModel} → {job2.VehicleModel}");
                if (nullChange) Console.WriteLine($"    👉 NULL status dəyişdi");
            }
            else
            {
                Console.WriteLine($"❌❌❌ DetectChanges RESULT: {hasChanges} - DƏYİŞİKLİK YOXDUR ❌❌❌");
            }
            
            Console.WriteLine($"🔍🔍🔍 DEBUG: DetectChanges TAMAMLANDI 🔍🔍🔍");
            
            return hasChanges;
        }

        private DateTime CalculateMidDate(DateTime date1, DateTime date2)
        {
            var totalTicks = (date1.Ticks + date2.Ticks) / 2;
            return new DateTime(totalTicks);
        }

        private async Task UpdateUserWithEstimatedDateAsync(int userId, InsuranceJob earlierJob, InsuranceJob laterJob)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return;

            // Orta nöqtəni hesabla
            var midDate = CalculateMidDate(earlierJob.CheckDate.Value, laterJob.CheckDate.Value);
            
            user.EstimatedRenewalDay = midDate.Day;
            user.EstimatedRenewalMonth = midDate.Month;
            user.LastConfirmedRenewalDate = midDate;
            // Yeni: pəncərə sərhədlərini saxla
            user.RenewalWindowStart = earlierJob.CheckDate?.Date;
            user.RenewalWindowEnd = laterJob.CheckDate?.Date;
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            // RenewalWindow lead yarat
            var lead = new Lead
            {
                UserId = userId,
                CarNumber = user.CarNumber,
                LeadType = "RenewalWindow",
                Notes = $"Yenilənmə tarixi: {midDate:dd/MM/yyyy}, Interval: {user.RenewalWindowStart:dd/MM} - {user.RenewalWindowEnd:dd/MM}"
            };
            
            _context.Leads.Add(lead);
            await _context.SaveChangesAsync();
            
            // Notification approval prosesi
            await _leadService.CreateNotificationForLeadAsync(lead);

            _logger.LogInformation("Updated user {UserId} with estimated renewal date: {Day}/{Month} and created RenewalWindow lead", 
                userId, user.EstimatedRenewalDay, user.EstimatedRenewalMonth);
        }

        #endregion

        #region Public Query Methods

        /// <summary>
        /// FinalCheck fazasındakı bütün tracking-ləri prosess edir
        /// </summary>
        public async Task ProcessFinalCheckTrackingsAsync()
        {
            var finalCheckTrackings = await _context.InsuranceRenewalTracking
                .Where(t => t.CurrentPhase == "FinalCheck")
                .Include(t => t.User)
                .ToListAsync();

            foreach (var tracking in finalCheckTrackings)
            {
                _logger.LogInformation("Processing FinalCheck tracking: {TrackingId}", tracking.Id);

                // TODO: Bildiriş planlaşdır (Step 5-də implementasiya ediləcək)
                // await _notificationService.ScheduleNotificationsAsync(tracking.User);

                // Prosesi tamamlandı kimi qeyd et
                tracking.CurrentPhase = "Completed";
                tracking.LastCheckResult = $"Prosess tamamlandı. Təxmini tarix: {tracking.User.EstimatedRenewalDay}/{tracking.User.EstimatedRenewalMonth}";
                tracking.UpdatedAt = DateTime.Now;
            }

            if (finalCheckTrackings.Any())
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Processed {Count} FinalCheck trackings", finalCheckTrackings.Count);
            }
        }

        /// <summary>
        /// Avtomobil üçün renewal tracking statusunu əldə edir
        /// </summary>
        public async Task<InsuranceRenewalTracking?> GetTrackingStatusAsync(string carNumber)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.CarNumber == carNumber);
            if (user == null) return null;

            return await _context.InsuranceRenewalTracking
                .Include(t => t.User)
                .Where(t => t.UserId == user.Id)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();
        }

        #endregion
    }
}
