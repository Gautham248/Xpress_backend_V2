namespace Xpress_backend_V2.Models.DTO
{
    public class UserTravelRequestDTO
    {
        public string RequestId { get; set; }
        public string Destination { get; set; }
        public DateTime OutboundDepartureDate { get; set; }
        public DateTime? ReturnDepartureDate { get; set; }
        public string PurposeOfTravel { get; set; }
        public string CurrentStatusName { get; set; }
        public int UserId { get; set; } // Added for completeness

        public DateTime CreatedAt { get; set; }
    }
}
