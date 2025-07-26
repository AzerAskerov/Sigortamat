using System;

namespace Sigortamat.Models
{
    /// <summary>
    /// Admin təsdiqi tələb edən bildirişlər
    /// </summary>
    public class Notification
    {
        public int Id { get; set; }
        public int LeadId { get; set; }
        public string Channel { get; set; } = "wa"; // whatsapp
        public string Message { get; set; } = string.Empty;
        public string Status { get; set; } = "pending"; // pending, approved, sent, error
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ApprovedAt { get; set; }
        public DateTime? SentAt { get; set; }
        
        // Navigation properties
        public Lead Lead { get; set; } = null!;
    }
} 