using System;

namespace SigortaYoxla.Models
{
    /// <summary>
    /// Sığorta yoxlama işləri üçün detallı model - ISB.az real data-ya uyğun
    /// </summary>
    public class InsuranceJob
    {
        public int Id { get; set; }
        public int QueueId { get; set; }
        
        // Car information
        public string CarNumber { get; set; } = ""; // Plate number (PlateNumber -> CarNumber)
        public string? VehicleBrand { get; set; } // BMW, Mercedes, vs.
        public string? VehicleModel { get; set; } // 520, E200, vs.
        
        // Insurance information 
        public string Status { get; set; } = ""; // "valid", "expired", "not_found"
        public string? Company { get; set; } // Insurance company name
        
        // ISB.az raw data
        public string? ResultText { get; set; } // Full raw result from ISB.az
        
        // Processing metadata
        public int? ProcessingTimeMs { get; set; } // Processing time in milliseconds
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ProcessedAt { get; set; }
        
        // Navigation property
        public Queue Queue { get; set; } = null!;
    }
}
