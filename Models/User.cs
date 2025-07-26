using System;
using System.Collections.Generic;

namespace Sigortamat.Models
{
    /// <summary>
    /// İstifadəçi məlumatları və sığorta yenilənmə tarix təxminləri
    /// </summary>
    public class User
    {
        public int Id { get; set; }
        public string CarNumber { get; set; } = "";
        public string? PhoneNumber { get; set; }
        public int? EstimatedRenewalDay { get; set; }
        public int? EstimatedRenewalMonth { get; set; }
        public DateTime? LastConfirmedRenewalDate { get; set; }
        /// <summary>
        /// Hesablanmış sığorta yenilənmə pəncərəsinin başlanğıcı (earlier job tarixidir)
        /// </summary>
        public DateTime? RenewalWindowStart { get; set; }

        /// <summary>
        /// Hesablanmış sığorta yenilənmə pəncərəsinin sonu (later job tarixidir)
        /// </summary>
        public DateTime? RenewalWindowEnd { get; set; }
        public bool NotificationEnabled { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public ICollection<Lead> Leads { get; set; } = new List<Lead>();
        public ICollection<InsuranceRenewalTracking> InsuranceRenewalTrackings { get; set; } = new List<InsuranceRenewalTracking>();
    }
}
