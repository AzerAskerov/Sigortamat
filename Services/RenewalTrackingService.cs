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
        private readonly ILogger<RenewalTrackingService> _logger;

        public RenewalTrackingService(
            ApplicationDbContext context,
            QueueRepository queueRepository,
            InsuranceJobRepository insuranceJobRepository,
            ILogger<RenewalTrackingService> logger)
        {
            _context = context;
            _queueRepository = queueRepository;
            _insuranceJobRepository = insuranceJobRepository;
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
            // Initial fazadan YearSearch fazasına keç
            tracking.CurrentPhase = "YearSearch";
            tracking.NextCheckDate = completedJob.CheckDate?.AddYears(-1) ?? DateTime.Now.AddYears(-1);

            // Növbəti job yarat
            await CreateInsuranceJobAsync(tracking, tracking.User.CarNumber, tracking.NextCheckDate.Value);

            _logger.LogInformation("Moved to YearSearch phase for tracking: {TrackingId}", tracking.Id);
        }

        private async Task ProcessYearSearchPhaseAsync(InsuranceRenewalTracking tracking, InsuranceJob completedJob)
        {
            Console.WriteLine($"🔍 DEBUG: ProcessYearSearchPhaseAsync - Job ID: {completedJob.Id}, CheckDate: {completedJob.CheckDate}");
            
            // Əvvəlki job-la müqayisə et
            var previousJob = await GetPreviousJobAsync(tracking.Id, completedJob.CheckDate);
            
            Console.WriteLine($"🔍 DEBUG: GetPreviousJobAsync nəticəsi - Previous Job: {(previousJob != null ? $"ID {previousJob.Id}, Date: {previousJob.CheckDate}" : "NULL")}");
            
            if (previousJob != null)
            {
                Console.WriteLine($"🔍 DEBUG: Müqayisə - Current: {completedJob.Company}/{completedJob.VehicleBrand}/{completedJob.VehicleModel}");
                Console.WriteLine($"🔍 DEBUG: Müqayisə - Previous: {previousJob.Company}/{previousJob.VehicleBrand}/{previousJob.VehicleModel}");
                
                bool hasChanges = DetectChanges(previousJob, completedJob);
                Console.WriteLine($"🔍 DEBUG: DetectChanges nəticəsi: {hasChanges}");

                if (hasChanges)
                {
                    Console.WriteLine($"✅ DEBUG: Dəyişiklik tapıldı! MonthSearch fazasına keçirik");
                    
                    // Dəyişiklik tapıldı - MonthSearch fazasına keç
                    tracking.CurrentPhase = "MonthSearch";
                    
                    // İkili axtarış üçün orta nöqtə hesabla
                    var midDate = CalculateMidDate(completedJob.CheckDate.Value, previousJob.CheckDate.Value);
                    tracking.NextCheckDate = midDate;
                    
                    Console.WriteLine($"🔍 DEBUG: MonthSearch üçün mid date: {midDate}");

                    await CreateInsuranceJobAsync(tracking, tracking.User.CarNumber, midDate);

                    _logger.LogInformation("Found changes, moved to MonthSearch phase for tracking: {TrackingId}", tracking.Id);
                    return;
                }
                else
                {
                    Console.WriteLine($"⚠️ DEBUG: Dəyişiklik tapılmadı, YearSearch-a davam");
                }
            }
            else
            {
                Console.WriteLine($"⚠️ DEBUG: Previous job tapılmadı, YearSearch-a davam");
            }

            // Dəyişiklik yoxdur - daha əvvələ get
            tracking.NextCheckDate = completedJob.CheckDate?.AddYears(-1) ?? DateTime.Now.AddYears(-2);
            Console.WriteLine($"🔍 DEBUG: Növbəti yoxlama tarixi: {tracking.NextCheckDate}");
            
            await CreateInsuranceJobAsync(tracking, tracking.User.CarNumber, tracking.NextCheckDate.Value);

            _logger.LogInformation("No changes found, continuing YearSearch for tracking: {TrackingId}", tracking.Id);
        }

        private async Task ProcessMonthSearchPhaseAsync(InsuranceRenewalTracking tracking, InsuranceJob completedJob)
        {
            // Bütün əlaqəli job-ları əldə et
            var allJobs = await GetAllRelatedJobsAsync(tracking.Id);
            var laterJobs = allJobs.Where(j => j.CheckDate > completedJob.CheckDate).OrderBy(j => j.CheckDate).ToList();

            if (!laterJobs.Any())
            {
                _logger.LogWarning("No later jobs found for MonthSearch phase, tracking: {TrackingId}", tracking.Id);
                return;
            }

            var nearestLater = laterJobs.First();
            var dateDiff = nearestLater.CheckDate.Value - completedJob.CheckDate.Value;

            if (dateDiff.TotalDays <= 31)
            {
                // 1 aydan az fərq - FinalCheck fazasına keç
                tracking.CurrentPhase = "FinalCheck";
                await UpdateUserWithEstimatedDateAsync(tracking.UserId, completedJob, nearestLater);

                _logger.LogInformation("Interval narrowed to 1 month, moved to FinalCheck phase for tracking: {TrackingId}", 
                    tracking.Id);
                return;
            }

            // İkili axtarışa davam et
            var nextDate = CalculateMidDate(completedJob.CheckDate.Value, nearestLater.CheckDate.Value);
            tracking.NextCheckDate = nextDate;
            await CreateInsuranceJobAsync(tracking, tracking.User.CarNumber, nextDate);

            _logger.LogInformation("Continuing binary search in MonthSearch phase for tracking: {TrackingId}", tracking.Id);
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
            
            var previousJob = await _context.InsuranceJobs
                .Where(j => j.InsuranceRenewalTrackingId == trackingId && 
                           j.CheckDate < currentCheckDate &&
                           j.Status == "completed")
                .OrderByDescending(j => j.CheckDate)
                .FirstOrDefaultAsync();
                
            Console.WriteLine($"🔍 DEBUG: GetPreviousJobAsync filter result - Previous Job: {(previousJob != null ? $"ID {previousJob.Id}, Status: {previousJob.Status}, Date: {previousJob.CheckDate}" : "NULL")}");
            
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
            Console.WriteLine($"🔍 DEBUG: DetectChanges - Job1: Company='{job1.Company}', Brand='{job1.VehicleBrand}', Model='{job1.VehicleModel}'");
            Console.WriteLine($"🔍 DEBUG: DetectChanges - Job2: Company='{job2.Company}', Brand='{job2.VehicleBrand}', Model='{job2.VehicleModel}'");
            
            bool companyChange = job1.Company != job2.Company;
            bool brandChange = job1.VehicleBrand != job2.VehicleBrand;
            bool modelChange = job1.VehicleModel != job2.VehicleModel;
            bool nullChange = string.IsNullOrEmpty(job1.Company) != string.IsNullOrEmpty(job2.Company);
            
            Console.WriteLine($"🔍 DEBUG: DetectChanges - Company change: {companyChange}, Brand change: {brandChange}, Model change: {modelChange}, Null change: {nullChange}");
            
            bool hasChanges = nullChange || companyChange || brandChange || modelChange;
            Console.WriteLine($"🔍 DEBUG: DetectChanges - Final result: {hasChanges}");
            
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
            user.UpdatedAt = DateTime.Now;

            _logger.LogInformation("Updated user {UserId} with estimated renewal date: {Day}/{Month}", 
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
