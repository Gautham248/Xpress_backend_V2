namespace Xpress_backend_V2.Models.DTO
{
    public class TravelRequestDTO
    {
        public string RequestId { get; set; }
        public string SourcePlace { get; set; }
        public string SourceCountry { get; set; }
        public string DestinationPlace { get; set; }
        public string DestinationCountry { get; set; }

        public DateTime OutboundDepartureDate { get; set; }
        public DateTime? OutboundArrivalDate { get; set; }
        public DateTime? ReturnDepartureDate { get; set; }
        public DateTime? ReturnArrivalDate { get; set; }

        public bool IsAccommodationRequired { get; set; }
        public bool IsPickupRequired { get; set; }
        public bool IsDropoffRequired { get; set; }

        public string PickupPlace { get; set; }
        public string DropoffPlace { get; set; }
        public string Comments { get; set; }
        public string PurposeOfTravel { get; set; }
        public bool IsVegetarian { get; set; }
        public bool AttendedCct { get; set; }

        public string? TravelAgencyName { get; set; }
        public decimal? TotalExpense { get; set; }
        public string? UploadedTicketPdfPath { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Display fields from related entities
        public string EmployeeName { get; set; }
        public bool IsInternational { get; set; }
        public bool IsRoundTrip { get; set; }
        public string ProjectName { get; set; }
        public string TravelModeName { get; set; }
        public string CurrentStatusName { get; set; }
        public int? SelectedTicketOptionId { get; set; }

        // New fields for DU ID and Project Manager Name
        public int DuId { get; set; }
        public string ProjectManagerName { get; set; }
    }
}
