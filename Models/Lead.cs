using System;
using System.Collections.Generic;

namespace Sigortamat.Models
{
    /// <summary>
    /// Potensial satış yönləndirilməsi (lead) – sığortası olmayan və ya yeniləmə vaxtı yaxınlaşan istifadəçilər.
    /// </summary>
    public class Lead
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string CarNumber { get; set; } = string.Empty;
        /// <summary>
        /// Lead növü (məs: NoInsuranceImmediate, RenewalWindow, Other)
        /// </summary>
        public string LeadType { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsConverted { get; set; } = false;
        
        // Navigation properties
        public User User { get; set; } = null!;
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
} 