using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sigortamat.Data;
using Sigortamat.Models;

namespace Sigortamat.Services
{
    /// <summary>
    /// Sığorta yoxlama işləri üçün repository
    /// </summary>
    public class InsuranceJobRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly QueueRepository _queueRepository;

        public InsuranceJobRepository(ApplicationDbContext context, QueueRepository queueRepository)
        {
            _context = context;
            _queueRepository = queueRepository;
        }

        /// <summary>
        /// Renewal tracking üçün sığorta yoxlama işi yarat
        /// </summary>
        public async Task<int> CreateInsuranceJobAsync(string carNumber, DateTime checkDate, 
            int? renewalTrackingId = null, DateTime? processAfter = null, int priority = 0)
        {
            int queueId = _queueRepository.AddToQueue("insurance", priority, processAfter);
            
            using var db = new ApplicationDbContextFactory().CreateDbContext(new string[0]);
            var insuranceJob = new InsuranceJob
            {
                QueueId = queueId,
                CarNumber = carNumber,
                Status = "pending",
                CheckDate = checkDate,
                InsuranceRenewalTrackingId = renewalTrackingId,
                CreatedAt = DateTime.Now
            };
            db.InsuranceJobs.Add(insuranceJob);
            await db.SaveChangesAsync();
            
            Console.WriteLine($"🚗 Renewal tracking üçün sığorta işi yaradıldı: {carNumber} (Tarix: {checkDate:yyyy-MM-dd})");
            return queueId;
        }

        /// <summary>
        /// Yeni sığorta yoxlama işi yarat
        /// </summary>
        public int CreateInsuranceJob(string carNumber, string? vehicleBrand = null, string? vehicleModel = null, int priority = 0)
        {
            int queueId = _queueRepository.AddToQueue("insurance", priority);
            
            using var db = new ApplicationDbContextFactory().CreateDbContext(new string[0]);
            var insuranceJob = new InsuranceJob
            {
                QueueId = queueId,
                CarNumber = carNumber,
                VehicleBrand = vehicleBrand,
                VehicleModel = vehicleModel,
                Status = "pending",
                CheckDate = DateTime.Now,  // CheckDate əlavə edildi
                CreatedAt = DateTime.Now
            };
            db.InsuranceJobs.Add(insuranceJob);
            db.SaveChanges();
            
            Console.WriteLine($"🚗 Yeni sığorta yoxlama işi yaradıldı: {carNumber} (Queue ID: {queueId})");
            return queueId;
        }

        /// <summary>
        /// InsuranceJob-u yenilə - async
        /// </summary>
        public async Task UpdateInsuranceJobAsync(InsuranceJob job)
        {
            using var db = new ApplicationDbContextFactory().CreateDbContext(new string[0]);
            db.InsuranceJobs.Update(job);
            await db.SaveChangesAsync();
        }
        
        /// <summary>
        /// Sığorta yoxlama nəticəsini yenilə
        /// </summary>
        public void UpdateInsuranceResult(int queueId, string status, 
            string? company = null, int? processingTimeMs = null, 
            string? vehicleBrand = null, string? vehicleModel = null, string? resultText = null)
        {
            using var db = new ApplicationDbContextFactory().CreateDbContext(new string[0]);
            var job = db.InsuranceJobs.FirstOrDefault(j => j.QueueId == queueId);
            if (job != null)
            {
                job.Status = status;
                job.Company = company;
                job.ProcessingTimeMs = processingTimeMs;
                job.VehicleBrand = vehicleBrand;
                job.VehicleModel = vehicleModel;
                job.ResultText = resultText;
                job.ProcessedAt = DateTime.Now;
                db.SaveChanges();
                
                Console.WriteLine($"✅ Sığorta yoxlama nəticəsi yeniləndi: {job.CarNumber} - {status}");
            }
        }
        
        /// <summary>
        /// Gözləyən sığorta işlərini gətir
        /// </summary>
        public List<InsuranceJob> GetPendingInsuranceJobs(int limit = 10)
        {
            using var db = new ApplicationDbContextFactory().CreateDbContext(new string[0]);
            return db.InsuranceJobs
                .Join(db.Queues, 
                    job => job.QueueId, 
                    queue => queue.Id, 
                    (job, queue) => new { Job = job, Queue = queue })
                .Where(x => x.Queue.Status == "pending")
                .OrderBy(x => x.Queue.Priority)
                .ThenBy(x => x.Queue.CreatedAt)
                .Take(limit)
                .Select(x => x.Job)
                .Include(j => j.Queue)
                .ToList();
        }
        
        /// <summary>
        /// Sığorta işini queue ID ilə gətir
        /// </summary>
        public InsuranceJob? GetInsuranceJobByQueueId(int queueId)
        {
            using var db = new ApplicationDbContextFactory().CreateDbContext(new string[0]);
            return db.InsuranceJobs
                .Include(j => j.Queue)
                .FirstOrDefault(j => j.QueueId == queueId);
        }
        
        /// <summary>
        /// Real test məlumatları yarat (işlək ISB.az nömrələri ilə)
        /// </summary>
        public void SeedRealTestData()
        {
            Console.WriteLine("🔍 Real ISB.az test məlumatları yaradılır...");
            
            // Real Azərbaycan avtomobil nömrələri (işlək commit-dən)
            CreateInsuranceJob("10RL035", "BMW", "520", 1); // Əsas test car number
            CreateInsuranceJob("10RL033", "Mercedes", "E200", 1); 
            CreateInsuranceJob("90HB986", "Toyota", "Camry", 1);
            
            // Əlavə real nömrələr
            CreateInsuranceJob("90AA123", "Nissan", "Altima", 0);
            CreateInsuranceJob("10BB456", "Hyundai", "Santa Fe", 0);
            
            Console.WriteLine("✅ Real ISB.az test məlumatları yaradıldı - Real Selenium aktiv!");
        }
        /// <summary>
        /// Renewal tracking ilə bağlı sığorta işlərini gətir
        /// </summary>
        public async Task<List<InsuranceJob>> GetRenewalTrackingJobsAsync(int renewalTrackingId)
        {
            using var db = new ApplicationDbContextFactory().CreateDbContext(new string[0]);
            return await db.InsuranceJobs
                .Where(j => j.InsuranceRenewalTrackingId == renewalTrackingId)
                .OrderBy(j => j.CheckDate)
                .ToListAsync();
        }

        public void ShowInsuranceStatistics()
        {
            using var db = new ApplicationDbContextFactory().CreateDbContext(new string[0]);
            
            var total = db.InsuranceJobs.Count();
            if (total == 0)
            {
                Console.WriteLine("📊 Sığorta işi statistikası: Heç bir məlumat yoxdur");
                return;
            }
            
            var completed = db.InsuranceJobs
                .Join(db.Queues, j => j.QueueId, q => q.Id, (j, q) => new { Job = j, Queue = q })
                .Count(x => x.Queue.Status == "completed");
                
            var avgProcessingTime = db.InsuranceJobs
                .Where(j => j.ProcessingTimeMs.HasValue)
                .Select(j => j.ProcessingTimeMs!.Value)
                .DefaultIfEmpty(0)
                .Average();
                
            var validInsurances = db.InsuranceJobs.Count(j => j.Status == "valid");
            var expiredInsurances = db.InsuranceJobs.Count(j => j.Status == "expired");
            
            Console.WriteLine("\n📊 SİGORTA STATİSTİKASI:");
            Console.WriteLine("=".PadRight(50, '='));
            Console.WriteLine($"📋 Ümumi: {total}");
            Console.WriteLine($"✅ Tamamlanmış: {completed}");
            Console.WriteLine($"⚡ Orta prosessing vaxtı: {avgProcessingTime:F0}ms");
            Console.WriteLine($"🛡️ Etibarlı sığorta: {validInsurances}");
            Console.WriteLine($"⚠️ Vaxtı bitmiş: {expiredInsurances}");
        }

        public async Task<InsuranceJob?> GetByQueueIdAsync(int queueId)
        {
            return await _context.InsuranceJobs.FirstOrDefaultAsync(j => j.QueueId == queueId);
        }

        /// <summary>
        /// InsuranceJob nəticəsini yenilə - DailyLimitExceeded halında status dəyişdirilmir
        /// </summary>
        public async Task UpdateJobResultAsync(int jobId, InsuranceResult result, long processingTime)
        {
            var job = await _context.InsuranceJobs.FindAsync(jobId);
            if (job != null)
            {
                // Daily limit zamanı status dəyişməsin, pending qalsın
                if (result.ResultText == "DailyLimitExceeded")
                {
                    // Status dəyişilmir (pending qalır)
                    job.ResultText = "Gündəlik limit doldu - sabah yenidən yoxlanılacaq";
                }
                else
                {
                    job.Status = result.Success ? "completed" : "failed";
                    job.ResultText = result.Success ? result.ResultText : result.ErrorMessage;
                }
                
                job.Company = result.Company;
                job.VehicleBrand = result.VehicleBrand;
                job.VehicleModel = result.VehicleModel;
                job.ProcessedAt = DateTime.Now;
                job.ProcessingTimeMs = (int)processingTime; // Cast to int
                
                await _context.SaveChangesAsync();
            }
        }
    }
}
