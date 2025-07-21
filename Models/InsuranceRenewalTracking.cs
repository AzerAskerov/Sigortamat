using System;

namespace Sigortamat.Models
{
    /// <summary>
    /// Sığorta yenilənmə tarixi izləmə prosesi məlumatları
    /// Fazalar: Initial → YearSearch → MonthSearch → FinalCheck → Completed
    /// </summary>
    public class InsuranceRenewalTracking
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        
        /// <summary>
        /// İzləmə prosesinin cari fazası
        /// Mümkün dəyərlər: "Initial", "YearSearch", "MonthSearch", "FinalCheck", "Completed"
        /// </summary>
        public string CurrentPhase { get; set; } = "Initial";
        
        public DateTime? LastCheckDate { get; set; }
        public DateTime? NextCheckDate { get; set; }
        public int ChecksPerformed { get; set; } = 0;
        public string? LastCheckResult { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public User User { get; set; } = null!;
    }
}
