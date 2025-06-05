using System.ComponentModel.DataAnnotations;

namespace Xpress_backend_V2.Models.DTO
{
    public class UpdateTicketOptionDTO
    {
        [Required]
        public string OptionDescription { get; set; }
    }
}
