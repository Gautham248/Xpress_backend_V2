namespace Xpress_backend_V2.Models.DTO
{
    public class TravelRequestCreateDTO
    {
        public int UserId { get; set; }
        public int TravelModeId { get; set; }
        public bool IsInternational { get; set; }
        public bool IsRoundTrip { get; set; }
        public string ProjectCode { get; set; }
        public string SourcePlace { get; set; }
        public string SourceCountry { get; set; }
        public string DestinationPlace { get; set; }
        public string DestinationCountry { get; set; }
        public DateTime OutboundDepartureDate { get; set; }
        public DateTime? OutboundArrivalDate { get; set; }
        public DateTime? ReturnDepartureDate { get; set; }
        public DateTime? ReturnArrivalDate { get; set; }
        public bool IsAccommodationRequired { get; set; }
        public bool IsDropOffRequired { get; set; } 
        public string? DropOffPlace { get; set; }
        public bool IsPickUpRequired { get; set; }
        public string? PickUpPlace { get; set; }
        public string? Comments { get; set; }
        public string PurposeOfTravel { get; set; }
        public bool IsVegetarian { get; set; }
        public string? FoodComment { get; set; }
        public bool AttendedCCT { get; set; }
        public string? LDCertificatePath { get; set; }
    }
}