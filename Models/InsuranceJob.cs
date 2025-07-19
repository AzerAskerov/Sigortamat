using System;

namespace Sigortamat.Models
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
        public DateTime? CheckDate { get; set; } // The date to check insurance for (can be past date for renewal tracking)
        public int? InsuranceRenewalTrackingId { get; set; } // Link to renewal tracking process
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ProcessedAt { get; set; }
        
        // Navigation property
        public Queue Queue { get; set; } = null!;
    }
}
