using System;

namespace SigortaYoxla.Models
{
    /// <summary>
    /// WhatsApp mesaj göndərmə işləri üçün detallı model
    /// </summary>
    public class WhatsAppJob
    {
        public int Id { get; set; }
        public int QueueId { get; set; }
        public string PhoneNumber { get; set; } = "";
        public string MessageText { get; set; } = "";
        public string DeliveryStatus { get; set; } = "pending"; // "pending", "sent", "delivered", "read", "failed"
        public DateTime? SentAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public string? ErrorDetails { get; set; }
        public int? ProcessingTimeMs { get; set; }
        
        // Navigation property
        public Queue Queue { get; set; } = null!;
    }
}
