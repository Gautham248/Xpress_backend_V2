namespace Xpress_backend_V2.Models.DTO
{
    public class CalendarTravelRequestDTO
    {
        public string RequestId { get; set; }
        public string EmployeeName { get; set; }
        public DateTime OutboundDepartureDate { get; set; }
        public DateTime? OutboundArrivalDate { get; set; }
        public DateTime? ReturnDepartureDate { get; set; }
        public DateTime? ReturnArrivalDate { get; set; }
        public string SourcePlace { get; set; }
        public string SourceCountry { get; set; }
        public string DestinationPlace { get; set; }
        public string DestinationCountry { get; set; }
        public string CurrentStatusName { get; set; }
    }
}

