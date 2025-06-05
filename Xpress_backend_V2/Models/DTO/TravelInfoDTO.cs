namespace Xpress_backend_V2.Models.DTO
{
    public class TravelInfoDTO
    {
        public string RequestId { get; set; }
        public DateTime OutboundDepartureDate { get; set; }
        public DateTime OutboundArrivalDate { get; set; }
        public DateTime? ReturnDepartureDate { get; set; }
        public DateTime? ReturnArrivalDate { get; set; }
        public string Transportation { get; set; }
        public bool IsInternational { get; set; }
        public DateTime RequestCreateDate { get; set; }
        public string PurposeOfTravel { get; set; }
        public bool IsAccommodationRequired { get; set; }
        public bool IsVegetarian{ get; set; }
        public bool IsDropOffRequired { get; set; }
        public bool IsPickUpRequired { get; set; }
    }
}
