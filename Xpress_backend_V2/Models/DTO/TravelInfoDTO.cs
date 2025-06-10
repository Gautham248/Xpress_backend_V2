namespace Xpress_backend_V2.Models.DTO
{
    public class TravelInfoDTO
    {
        public string RequestId { get; set; }
        public DateTime OutboundDepartureDate { get; set; }
        public DateTime? OutboundArrivalDate { get; set; }
        public DateTime? ReturnDepartureDate { get; set; }
        public DateTime? ReturnArrivalDate { get; set; }
        public string Transportation { get; set; }
        public bool IsInternational { get; set; }
        public DateTime RequestCreateDate { get; set; }
        public string PurposeOfTravel { get; set; }
        public bool IsAccommodationRequired { get; set; }
        public bool IsVegetarian{ get; set; }
        public string DropOffLocation { get; set; }
        public string PickUpLocation { get; set; }
        public string? TravelFeedback { get; set; }
        public List<CommentDTO> Comments { get; set; }

        public string TravelerName { get; set; }    // To get initial comments
        public string? InitialComments { get; set; }
        public string? FoodComment { get; set; }
    }
}
