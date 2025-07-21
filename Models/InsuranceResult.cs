using System;

namespace Sigortamat.Models
{
    public class InsuranceResult
    {
        public bool Success { get; set; }
        public string? CarNumber { get; set; }
        public string? Company { get; set; }
        public string? OwnerName { get; set; }
        public string? VehicleBrand { get; set; }
        public string? VehicleModel { get; set; }
        public string? ResultText { get; set; }
        public string? ErrorMessage { get; set; }
        public long DurationMs { get; set; }
    }
}
