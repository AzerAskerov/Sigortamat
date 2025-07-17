using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SigortaYoxla.Data;
using SigortaYoxla.Models;

namespace SigortaYoxla.Services
{
    /// <summary>
    /// Queue repository - Yalnız yeni queue sistemi
    /// </summary>
    public class QueueRepository
    {
        #region Yeni Sistem - Queue Management

        /// <summary>
        /// Yeni queue elementi yarat
        /// </summary>
        public static int AddToQueue(string type, int priority = 0)
        {
            using var db = new ApplicationDbContextFactory().CreateDbContext(new string[0]);
            var queue = new Queue
            {
                Type = type,
                Status = "pending",
                Priority = priority,
                CreatedAt = DateTime.Now
            };
            
            db.Queues.Add(queue);
            db.SaveChanges();
            
            Console.WriteLine($"🔗 Queue yaradıldı: {type} (ID: {queue.Id}, Priority: {priority})");
            return queue.Id;
        }

        /// <summary>
        /// Queue elementini işlənir statusuna keçir
        /// </summary>
        public static void MarkAsProcessing(int queueId)
        {
            using var db = new ApplicationDbContextFactory().CreateDbContext(new string[0]);
            var queue = db.Queues.Find(queueId);
            if (queue != null)
            {
                queue.Status = "processing";
                queue.StartedAt = DateTime.Now;
                db.SaveChanges();
            }
        }

        /// <summary>
        /// Queue elementini tamamlanmış kimi işarələ
        /// </summary>
        public static void MarkAsCompleted(int queueId)
        {
            using var db = new ApplicationDbContextFactory().CreateDbContext(new string[0]);
            var queue = db.Queues.Find(queueId);
            if (queue != null)
            {
                queue.Status = "completed";
                queue.CompletedAt = DateTime.Now;
                db.SaveChanges();
                
                Console.WriteLine($"✅ Queue tamamlandı: ID {queueId}");
            }
        }

        /// <summary>
        /// Queue elementini uğursuz kimi işarələ
        /// </summary>
        public static void MarkAsFailed(int queueId, string errorMessage)
        {
            using var db = new ApplicationDbContextFactory().CreateDbContext(new string[0]);
            var queue = db.Queues.Find(queueId);
            if (queue != null)
            {
                queue.Status = "failed";
                queue.ErrorMessage = errorMessage;
                queue.RetryCount++;
                queue.CompletedAt = DateTime.Now;
                db.SaveChanges();
                
                Console.WriteLine($"❌ Queue uğursuz: ID {queueId} - {errorMessage}");
            }
        }

        /// <summary>
        /// Gözləyən queue elementlərini gətir
        /// </summary>
        public static List<Queue> GetPendingQueues(string type, int limit = 10)
        {
            using var db = new ApplicationDbContextFactory().CreateDbContext(new string[0]);
            return db.Queues
                       .Where(q => q.Type == type && q.Status == "pending")
                       .OrderBy(q => q.Priority)
                       .ThenBy(q => q.CreatedAt)
                       .Take(limit)
                       .ToList();
        }

        #endregion

        /// <summary>
        /// Bütün queue elementlərinin statusunu göstər
        /// </summary>
        public static void ShowQueueStatus()
        {
            Console.WriteLine("\n📊 QUEUE STATUS:");
            Console.WriteLine("=".PadRight(50, '='));
            
            using var db = new ApplicationDbContextFactory().CreateDbContext(new string[0]);
            if (db.Queues.Any())
            {
                var newTotal = db.Queues.Count();
                var newCompleted = db.Queues.Count(q => q.Status == "completed");
                var newFailed = db.Queues.Count(q => q.Status == "failed");
                var newPending = db.Queues.Count(q => q.Status == "pending");
                var newProcessing = db.Queues.Count(q => q.Status == "processing");

                Console.WriteLine($"📋 Ümumi: {newTotal}");
                Console.WriteLine($"✅ Tamamlanmış: {newCompleted}");
                Console.WriteLine($"❌ Uğursuz: {newFailed}");
                Console.WriteLine($"🔄 İşlənir: {newProcessing}");
                Console.WriteLine($"⏳ Gözləyən: {newPending}");

                if (newPending > 0)
                {
                    Console.WriteLine("\n⏳ GÖZLƏYƏN QUEUE-LAR:");
                    var pendingQueues = db.Queues.Where(q => q.Status == "pending").ToList();
                    foreach (var queue in pendingQueues)
                    {
                        Console.WriteLine($"  {queue.Id}. {queue.Type.ToUpper()} - Priority: {queue.Priority}");
                    }
                }
            }
            else
            {
                Console.WriteLine("📋 Heç bir queue məlumatı yoxdur");
            }
        }
    }
}
