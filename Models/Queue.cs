using System;

namespace Sigortamat.Models
{
    /// <summary>
    /// Ümumi queue sistemi üçün model
    /// </summary>
    public class Queue
    {
        public int Id { get; set; }
        public string Type { get; set; } = ""; // "insurance", "whatsapp", etc.
        public string Status { get; set; } = "pending"; // "pending", "processing", "completed", "failed"
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public int Priority { get; set; } = 0;
        public int RetryCount { get; set; } = 0;
        public string? ErrorMessage { get; set; }
    }
}
