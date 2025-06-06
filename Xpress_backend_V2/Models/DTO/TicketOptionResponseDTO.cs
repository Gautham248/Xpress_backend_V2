namespace Xpress_backend_V2.Models.DTO
{
    public class TicketOptionResponseDTO
    {
        public int OptionId { get; set; }
        public string RequestId { get; set; }
        public int CreatedByUserId { get; set; }
        public string OptionDescription { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsSelected { get; set; }
    }
}
