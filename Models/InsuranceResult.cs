using System;

namespace Sigortamat.Models
{
    /// <summary>
    /// Sığorta yoxlama nəticəsi üçün model
    /// </summary>
    public class InsuranceResult
    {
        public bool IsValid { get; set; }
        public string Status { get; set; } = "";
        public string? Company { get; set; }
        public string? OwnerName { get; set; }
        public decimal? Amount { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ResultText { get; set; } // For different result types: "Sığorta məlumatları tapıldı", "Məlumat tapılmadı", "DailyLimitExceeded", "Error", etc.
        public long? ProcessingTimeMs { get; set; } // Processing time in milliseconds
        
        // Real ISB.az məlumatları üçün əlavə sahələr
        public string? VehicleBrand { get; set; }
        public string? VehicleModel { get; set; }
        public string? FullResultText { get; set; }
        
        /// <summary>
        /// Uğurlu nəticə yarat
        /// </summary>
        public static InsuranceResult Success(string company, string ownerName, decimal amount)
        {
            return new InsuranceResult
            {
                IsValid = true,
                Status = "Aktiv",
                Company = company,
                OwnerName = ownerName,
                Amount = amount
            };
        }
        
        /// <summary>
        /// Xəta nəticəsi yarat
        /// </summary>
        public static InsuranceResult Error(string errorMessage)
        {
            return new InsuranceResult
            {
                IsValid = false,
                Status = "Xəta",
                ErrorMessage = errorMessage
            };
        }
        
        /// <summary>
        /// Tapılmadı nəticəsi
        /// </summary>
        public static InsuranceResult NotFound()
        {
            return new InsuranceResult
            {
                IsValid = false,
                Status = "Tapılmadı",
                ErrorMessage = "Sığorta məlumatı tapılmadı"
            };
        }
    }
}
