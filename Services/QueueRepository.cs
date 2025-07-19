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
            return AddToQueue(type, priority, null);
        }

        /// <summary>
        /// Yeni queue elementi yarat - ProcessAfter ilə
        /// </summary>
        public static int AddToQueue(string type, int priority = 0, DateTime? processAfter = null)
        {
            using var db = new ApplicationDbContextFactory().CreateDbContext(new string[0]);
            var queue = new Queue
            {
                Type = type,
                Status = "pending",
                Priority = priority,
                ProcessAfter = processAfter,
                CreatedAt = DateTime.Now
            };
            
            db.Queues.Add(queue);
            db.SaveChanges();
            
            string processAfterInfo = processAfter.HasValue ? $" (Process After: {processAfter:dd.MM.yyyy HH:mm})" : "";
            Console.WriteLine($"🔗 Queue yaradıldı: {type} (ID: {queue.Id}, Priority: {priority}){processAfterInfo}");
            return queue.Id;
        }

        /// <summary>
        /// Queue elementi tap - async
        /// </summary>
        public async Task<Queue?> GetQueueAsync(int queueId)
        {
            using var db = new ApplicationDbContextFactory().CreateDbContext(new string[0]);
            return await db.Queues.FindAsync(queueId);
        }

        /// <summary>
        /// Queue elementini yenilə - async
        /// </summary>
        public async Task UpdateQueueAsync(Queue queue)
        {
            using var db = new ApplicationDbContextFactory().CreateDbContext(new string[0]);
            db.Queues.Update(queue);
            await db.SaveChangesAsync();
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
                // ProcessAfter field-ini preserve et
                var existingProcessAfter = queue.ProcessAfter;
                queue.Status = "processing";
                queue.StartedAt = DateTime.Now;
                // ProcessAfter-i yenidən təyin et (əgər varsa)
                if (existingProcessAfter.HasValue)
                {
                    queue.ProcessAfter = existingProcessAfter;
                }
                db.SaveChanges();
                
                Console.WriteLine($"🔧 DEBUG MarkAsProcessing - Queue {queueId}: Status=processing, ProcessAfter={queue.ProcessAfter}");
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
        /// Gözləyən queue elementlərini gətir - ProcessAfter sahəsini nəzərə alır
        /// </summary>
        public static List<Queue> GetPendingQueues(string type, int limit = 10)
        {
            using var db = new ApplicationDbContextFactory().CreateDbContext(new string[0]);
            var now = DateTime.Now;
            
            return db.Queues
                       .Where(q => q.Type == type && 
                                 q.Status == "pending" &&
                                 (q.ProcessAfter == null || q.ProcessAfter <= now))
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
                    var pendingQueues = db.Queues.Where(q => q.Status == "pending")
                                                 .OrderBy(q => q.Priority)
                                                 .ThenBy(q => q.CreatedAt)
                                                 .ToList();
                    
                    foreach (var queue in pendingQueues)
                    {
                        string processAfterInfo = "";
                        if (queue.ProcessAfter.HasValue)
                        {
                            if (queue.ProcessAfter > DateTime.Now)
                            {
                                processAfterInfo = $" [⏰ {queue.ProcessAfter:dd.MM.yyyy HH:mm}]";
                            }
                            else
                            {
                                processAfterInfo = " [✅ Hazır]";
                            }
                        }
                        
                        Console.WriteLine($"  {queue.Id}. {queue.Type.ToUpper()} - Priority: {queue.Priority}{processAfterInfo}");
                    }
                }
            }
            else
            {
                Console.WriteLine("📋 Heç bir queue məlumatı yoxdur");
            }
        }
        
        /// <summary>
        /// Queue-u gələcək tarixə yenidən planlaşdır
        /// Status-u pending-ə qaytarır və ProcessAfter set edir
        /// RAW SQL istifadə edir - EF Context problemini həll edir
        /// </summary>
        public static void RescheduleJob(int queueId, DateTime processAfter, string reason = "")
        {
            try
            {
                using var db = new ApplicationDbContextFactory().CreateDbContext(new string[0]);
                
                Console.WriteLine($"🔧 DEBUG RescheduleJob - Queue {queueId} üçün ProcessAfter set edilir: {processAfter:yyyy-MM-dd HH:mm:ss}");
                
                // ADO.NET ilə birbaşa SQL update - commit təmin edilir
                var connectionString = db.Database.GetDbConnection().ConnectionString;
                db.Dispose();
                
                using var sqlConn = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
                sqlConn.Open();
                using var sqlCmd = sqlConn.CreateCommand();
                sqlCmd.CommandText = @"
                    UPDATE Queues
                    SET Status = 'pending',
                        ProcessAfter = @processAfter,
                        ErrorMessage = @reason,
                        RetryCount = RetryCount + 1
                    WHERE Id = @queueId";
                sqlCmd.Parameters.AddWithValue("@processAfter", processAfter);
                sqlCmd.Parameters.AddWithValue("@reason", reason ?? string.Empty);
                sqlCmd.Parameters.AddWithValue("@queueId", queueId);
                var updated = sqlCmd.ExecuteNonQuery();
                Console.WriteLine($"🔧 DEBUG RescheduleJob - ADO.NET update result: {updated} sətir yeniləndi");
                Console.WriteLine($"⏰ Queue {queueId} ADO.NET ilə sabaha planlaşdırıldı: {processAfter:dd.MM.yyyy HH:mm} ({reason})");
                sqlConn.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ DEBUG RescheduleJob - Exception: {ex.Message}");
                Console.WriteLine($"❌ Stack Trace: {ex.StackTrace}");
            }
        }
    }
}
