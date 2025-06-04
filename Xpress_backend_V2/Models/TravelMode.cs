namespace Xpress_backend_V2.Models
{
    public class TravelMode
    {
        public int TravelModeId { get; set; } // PK
        public string TravelModeName { get; set; }

        // Navigation property
        public ICollection<TravelRequest> TravelRequests { get; set; }
    }
}
