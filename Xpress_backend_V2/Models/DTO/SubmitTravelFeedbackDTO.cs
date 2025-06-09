using System.ComponentModel.DataAnnotations;

namespace Xpress_backend_V2.Models.DTO
{
    public class SubmitTravelFeedbackDTO
    {
        [Required]
        public string FeedbackText { get; set; }

        [Required]
        public int SubmittingUserId { get; set; }
    }
}