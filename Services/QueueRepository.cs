using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sigortamat.Data;
using Sigortamat.Models;
using Microsoft.Extensions.Logging;

namespace Sigortamat.Services
{
    public interface IQueueRepository
    {
        Task<int> CreateQueueAsync(string type, int priority = 1, DateTime? processAfter = null);
        void MarkAsCompleted(int queueId);
        void MarkAsFailed(int queueId, string errorMessage);
        void RescheduleJob(int queueId, DateTime processAfter, string? errorMessage = null);
        Queue? GetQueueById(int queueId);
        Queue? DequeueAndMarkAsProcessing(string type);
    }

    /// <summary>
    /// Queue repository - Yalnız yeni queue sistemi
    /// </summary>
    public class QueueRepository : IQueueRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<QueueRepository> _logger;

        public QueueRepository(ApplicationDbContext context, ILogger<QueueRepository> logger)
        {
            _logger = logger;
            _context = context;
        }

        #region Yeni Sistem - Queue Management

        /// <summary>
        /// Yeni queue elementi yarat
        /// </summary>
        public int AddToQueue(string type, int priority = 0)
        {
            return AddToQueue(type, priority, null);
        }

        /// <summary>
        /// Yeni queue elementi yarat - ProcessAfter ilə
        /// </summary>
        public int AddToQueue(string type, int priority = 0, DateTime? processAfter = null)
        {
            return AddToQueueAsync(type, priority, processAfter).Result;
        }

        /// <summary>
        /// Yeni queue elementi yarat - ProcessAfter ilə (async)
        /// </summary>
        public async Task<int> AddToQueueAsync(string type, int priority = 0, DateTime? processAfter = null)
        {
            var queue = new Queue
            {
                Type = type,
                Status = "pending",
                Priority = priority,
                ProcessAfter = processAfter,
                CreatedAt = DateTime.Now
            };
            
            _context.Queues.Add(queue);
            await _context.SaveChangesAsync();
            
            string processAfterInfo = processAfter.HasValue ? $" (Process After: {processAfter:dd.MM.yyyy HH:mm})" : "";
            Console.WriteLine($"🔗 Queue yaradıldı: {type} (ID: {queue.Id}, Priority: {priority}){processAfterInfo}");
            return queue.Id;
        }

        /// <summary>
        /// Queue elementi tap - async
        /// </summary>
        public async Task<Queue?> GetQueueAsync(int queueId)
        {
            return await _context.Queues.FindAsync(queueId);
        }

        /// <summary>
        /// Queue elementini yenilə - async
        /// </summary>
        public async Task UpdateQueueAsync(Queue queue)
        {
            _context.Queues.Update(queue);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Queue elementini işlənir statusuna keçir
        /// </summary>
        public void MarkAsProcessing(int queueId)
        {
            var queue = _context.Queues.Find(queueId);
            if (queue != null)
            {
                var existingProcessAfter = queue.ProcessAfter;
                queue.Status = "processing";
                queue.StartedAt = DateTime.Now;
                if (existingProcessAfter.HasValue)
                {
                    queue.ProcessAfter = existingProcessAfter;
                }
                _context.SaveChanges();
                
                Console.WriteLine($"🔧 DEBUG MarkAsProcessing - Queue {queueId}: Status=processing, ProcessAfter={queue.ProcessAfter}");
            }
        }

        /// <summary>
        /// Queue elementini tamamlanmış kimi işarələ
        /// </summary>
        public void MarkAsCompleted(int queueId)
        {
            try
            {
                var queue = _context.Queues.Find(queueId);
                if (queue != null)
                {
                    Console.WriteLine($"🔧 DEBUG MarkAsCompleted - Queue {queueId}: Status={queue.Status} -> completed");
                    
                    queue.Status = "completed";
                    queue.CompletedAt = DateTime.Now;
                    var result = _context.SaveChanges();
                    
                    Console.WriteLine($"✅ Queue tamamlandı: ID {queueId}, SaveChanges result: {result}");
                    
                    // Yenidən yoxla
                    var updatedQueue = _context.Queues.Find(queueId);
                    Console.WriteLine($"🔧 DEBUG MarkAsCompleted - Queue {queueId} after update: Status={updatedQueue?.Status}");
                }
                else
                {
                    Console.WriteLine($"❌ DEBUG MarkAsCompleted - Queue {queueId} tapılmadı");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ DEBUG MarkAsCompleted - Exception: {ex.Message}");
                Console.WriteLine($"❌ Stack Trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Queue elementini ID-yə görə tap
        /// </summary>
        public Queue? GetQueueById(int queueId)
        {
            return _context.Queues.Find(queueId);
        }

        /// <summary>
        /// Queue elementini uğursuz kimi işarələ
        /// </summary>
        public void MarkAsFailed(int queueId, string errorMessage)
        {
            try
            {
                var queue = _context.Queues.Find(queueId);
                if (queue != null)
                {
                    Console.WriteLine($"🔧 DEBUG MarkAsFailed - Queue {queueId}: Status={queue.Status} -> failed");
                    
                    queue.Status = "failed";
                    queue.ErrorMessage = errorMessage;
                    queue.RetryCount++;
                    queue.CompletedAt = DateTime.Now;
                    var result = _context.SaveChanges();
                    
                    Console.WriteLine($"❌ Queue uğursuz: ID {queueId} - {errorMessage}, SaveChanges result: {result}");
                    
                    // Yenidən yoxla
                    var updatedQueue = _context.Queues.Find(queueId);
                    Console.WriteLine($"🔧 DEBUG MarkAsFailed - Queue {queueId} after update: Status={updatedQueue?.Status}");
                }
                else
                {
                    Console.WriteLine($"❌ DEBUG MarkAsFailed - Queue {queueId} tapılmadı");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ DEBUG MarkAsFailed - Exception: {ex.Message}");
                Console.WriteLine($"❌ Stack Trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Gözləyən queue elementlərini gətir - ProcessAfter sahəsini nəzərə alır
        /// </summary>
        public List<Queue> GetPendingQueues(string type, int limit = 10)
        {
            // Timezone kompensasiyası ilə SQL query istifadə et
            return _context.Queues
                       .FromSqlRaw("SELECT TOP ({1}) * FROM Queues WHERE Type = {0} AND Status = 'pending' AND (ProcessAfter IS NULL OR ProcessAfter <= DATEADD(HOUR, 4, GETDATE())) ORDER BY Priority, CreatedAt", type, limit)
                       .ToList();
        }

        #endregion

        /// <summary>
        /// Bütün queue elementlərinin statusunu göstər
        /// </summary>
        public void ShowQueueStatus()
        {
            Console.WriteLine("\n📊 QUEUE STATUS:");
            Console.WriteLine("=".PadRight(50, '='));
            
            if (_context.Queues.Any())
            {
                var newTotal = _context.Queues.Count();
                var newCompleted = _context.Queues.Count(q => q.Status == "completed");
                var newFailed = _context.Queues.Count(q => q.Status == "failed");
                var newPending = _context.Queues.Count(q => q.Status == "pending");
                var newProcessing = _context.Queues.Count(q => q.Status == "processing");

                Console.WriteLine($"📋 Ümumi: {newTotal}");
                Console.WriteLine($"✅ Tamamlanmış: {newCompleted}");
                Console.WriteLine($"❌ Uğursuz: {newFailed}");
                Console.WriteLine($"🔄 İşlənir: {newProcessing}");
                Console.WriteLine($"⏳ Gözləyən: {newPending}");

                if (newPending > 0)
                {
                    Console.WriteLine("\n⏳ GÖZLƏYƏN QUEUE-LAR:");
                    var pendingQueues = _context.Queues.Where(q => q.Status == "pending")
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
        /// Mövcud queue-nu gələcək tarix üçün yenidən planlaşdır
        /// </summary>
        public void RescheduleJob(int queueId, DateTime processAfter, string? errorMessage = null)
        {
            try
            {
                Console.WriteLine($"🔧 DEBUG RescheduleJob - Queue {queueId} üçün ProcessAfter set edilir: {processAfter:yyyy-MM-dd HH:mm:ss}");
                
                var queue = _context.Queues.Find(queueId);
                if (queue != null)
                {
                    queue.Status = "pending";
                    queue.ProcessAfter = processAfter;
                    queue.ErrorMessage = errorMessage ?? string.Empty;
                    queue.RetryCount++;
                    
                    _context.SaveChanges();
                    
                    Console.WriteLine($"✅ Queue {queueId} EF Core ilə sabaha planlaşdırıldı: {processAfter:dd.MM.yyyy HH:mm} ({errorMessage})");
                }
                else
                {
                    Console.WriteLine($"❌ Queue {queueId} tapılmadı");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ DEBUG RescheduleJob - Exception: {ex.Message}");
                Console.WriteLine($"❌ Stack Trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Atomically dequeues the next available job and marks it as 'processing'.
        /// This method uses a transaction and SQL locking to prevent race conditions.
        /// </summary>
        /// <param name="type">The type of queue to dequeue from.</param>
        /// <returns>The dequeued queue item, or null if none are available.</returns>
        public Queue? DequeueAndMarkAsProcessing(string type)
        {
            try
            {
                using var transaction = _context.Database.BeginTransaction();
                
                var queueItem = _context.Queues
                    .FromSqlRaw("SELECT TOP 1 * FROM Queues WITH (UPDLOCK, ROWLOCK, READPAST) WHERE Type = {0} AND Status = 'pending' AND (ProcessAfter IS NULL OR ProcessAfter <= DATEADD(HOUR, 4, GETDATE())) ORDER BY Priority, CreatedAt", type)
                    .ToList() // Execute the query and bring results into memory
                    .FirstOrDefault();

                if (queueItem != null)
                {
                    queueItem.Status = "processing";
                    queueItem.UpdatedAt = DateTime.Now;
                    _context.SaveChanges();
                    transaction.Commit();
                    
                    _logger.LogInformation("Dequeued and marked as processing: Queue ID {QueueId}", queueItem.Id);
                    return queueItem;
                }

                transaction.Rollback();
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while dequeuing job for type {Type}", type);
                return null;
            }
        }

        public async Task<int> CreateQueueAsync(string type, int priority = 1, DateTime? processAfter = null)
        {
            var queue = new Queue
            {
                Type = type,
                Status = "pending",
                Priority = priority,
                ProcessAfter = processAfter,
                CreatedAt = DateTime.Now
            };

            _context.Queues.Add(queue);
            await _context.SaveChangesAsync();
            return queue.Id;
        }
    }
}
