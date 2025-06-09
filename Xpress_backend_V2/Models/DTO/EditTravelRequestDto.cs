using System.ComponentModel.DataAnnotations;

namespace Xpress_backend_V2.Models.DTO
{
    public class EditTravelRequestDto
    {
        [Required]
        public int TravelModeId { get; set; }

        [Required]
        public bool IsInternational { get; set; }

        [Required]
        public bool IsRoundTrip { get; set; }

        [Required]
        public string ProjectCode { get; set; }

        [Required]
        public string SourcePlace { get; set; }

        [Required]
        public string SourceCountry { get; set; }

        [Required]
        public string DestinationPlace { get; set; }

        [Required]
        public string DestinationCountry { get; set; }

        [Required]
        public DateTime OutboundDepartureDate { get; set; }

     
        public DateTime? OutboundArrivalDate { get; set; }

        public DateTime? ReturnDepartureDate { get; set; }
        public DateTime? ReturnArrivalDate { get; set; }

        [Required]
        public bool IsAccommodationRequired { get; set; }

        [Required]
        public bool IsDropOffRequired { get; set; }
        public string? DropOffPlace { get; set; }

        [Required]
        public bool IsPickUpRequired { get; set; }
        public string? PickUpPlace { get; set; }

        public string? Comments { get; set; }

        [Required]
        public string PurposeOfTravel { get; set; }

        [Required]
        public bool IsVegetarian { get; set; }
        public string? FoodComment { get; set; }

   
        public bool? AttendedCCT { get; set; }

        public string? LDCertificatePath { get; set; }
    
}
}
