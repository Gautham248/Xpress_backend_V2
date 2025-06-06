namespace Xpress_backend_V2.Models.DTO
{
    public class TravelRequest_EmployeeDashboardDTO
  
    {
        public string Id { get; set; } // Maps to RequestId
        public string Destination { get; set; }
        public string DepartureDate { get; set; } // ISO date string (OutboundDepartureDate)
        public string? ReturnDate { get; set; } // ISO date string (ReturnArrivalDate, nullable)
        public string Purpose { get; set; }
        public string Status { get; set; } // Status name (e.g., "Pending", "Approved")
        public int CurrentStatusId { get; set; } // Used for status mapping in the service
    }
}
