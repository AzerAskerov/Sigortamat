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
        public int? RefId { get; set; } // Reference to related entity (e.g., NotificationId)
        public string? CarNumber { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Message { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public DateTime? ProcessAfter { get; set; } // İşə yalnız bu vaxtdan sonra başla
        public int Priority { get; set; } = 0;
        public int RetryCount { get; set; } = 0;
        public string? ErrorMessage { get; set; }
        public DateTime? UpdatedAt { get; set; } // Bu sətir əlavə olundu
    }
}
