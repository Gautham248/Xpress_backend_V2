namespace Xpress_backend_V2.Models
{
    public class TicketOption
    {
        public int OptionId { get; set; } // PK
        public string RequestId { get; set; } // FK → TravelRequests
        public int CreatedByUserId { get; set; } // FK → Users
        public string OptionDescription { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsSelected { get; set; } = false;

        // Navigation properties
        public TravelRequest TravelRequest { get; set; }
        public User CreatedByUser { get; set; }
        public ICollection<TravelRequest> SelectedByTravelRequests { get; set; }
    }
}
