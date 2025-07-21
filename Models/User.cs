using System;

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
        public bool NotificationEnabled { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}
