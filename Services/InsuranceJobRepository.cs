using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SigortaYoxla.Data;
using SigortaYoxla.Models;

namespace SigortaYoxla.Services
{
    /// <summary>
    /// Sığorta yoxlama işləri üçün repository
    /// </summary>
    public static class InsuranceJobRepository
    {
        /// <summary>
        /// Yeni sığorta yoxlama işi yarat
        /// </summary>
        public static int CreateInsuranceJob(string carNumber, string? vehicleBrand = null, string? vehicleModel = null, int priority = 0)
        {
            int queueId = QueueRepository.AddToQueue("insurance", priority);
            
            using var db = new ApplicationDbContextFactory().CreateDbContext(new string[0]);
            var insuranceJob = new InsuranceJob
            {
                QueueId = queueId,
                CarNumber = carNumber,
                VehicleBrand = vehicleBrand,
                VehicleModel = vehicleModel,
                Status = "pending",
                CreatedAt = DateTime.Now
            };
            db.InsuranceJobs.Add(insuranceJob);
            db.SaveChanges();
            
            Console.WriteLine($"🚗 Yeni sığorta yoxlama işi yaradıldı: {carNumber} (Queue ID: {queueId})");
            return queueId;
        }
        
        /// <summary>
        /// Sığorta yoxlama nəticəsini yenilə
        /// </summary>
        public static void UpdateInsuranceResult(int queueId, string status, 
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
        public static List<InsuranceJob> GetPendingInsuranceJobs(int limit = 10)
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
        public static InsuranceJob? GetInsuranceJobByQueueId(int queueId)
        {
            using var db = new ApplicationDbContextFactory().CreateDbContext(new string[0]);
            return db.InsuranceJobs
                .Include(j => j.Queue)
                .FirstOrDefault(j => j.QueueId == queueId);
        }
        
        /// <summary>
        /// Real test məlumatları yarat (işlək ISB.az nömrələri ilə)
        /// </summary>
        public static void SeedRealTestData()
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
        public static void ShowInsuranceStatistics()
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
    }
}
