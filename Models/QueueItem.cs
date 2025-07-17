using System;

namespace SigortaYoxla.Models
{
    /// <summary>
    /// Queue elementləri üçün model
    /// </summary>
    public class QueueItem
    {
        public int Id { get; set; }
        public string Type { get; set; } = ""; // "insurance" və ya "whatsapp"
        public string CarNumber { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string Message { get; set; } = "";
        public bool IsProcessed { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ProcessedAt { get; set; }
        public string? Error { get; set; }
    }
}
