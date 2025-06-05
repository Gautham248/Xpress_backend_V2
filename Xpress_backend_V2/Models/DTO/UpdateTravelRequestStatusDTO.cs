namespace Xpress_backend_V2.Models.DTO
{
    public class UpdateTravelRequestStatusDTO
    {
        public string RequestId { get; set; }
        public int NewStatusId { get; set; }
        public int UserId { get; set; }
        public string Comments { get; set; } = string.Empty;
        public string ActionType { get; set; } = "STATUS_UPDATED";
    }
}
