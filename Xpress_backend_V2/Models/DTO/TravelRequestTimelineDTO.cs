namespace Xpress_backend_V2.Models.DTO
{
    public class TravelRequestTimelineDTO
    {
        public string Status { get; set; } = string.Empty;
        public string RequestDate { get; set; } = string.Empty;
        public string TravelerName { get; set; } = "Traveler";
        public List<TimelineEventDTO> TimelineEvents { get; set; } = new List<TimelineEventDTO>();
    }
}
