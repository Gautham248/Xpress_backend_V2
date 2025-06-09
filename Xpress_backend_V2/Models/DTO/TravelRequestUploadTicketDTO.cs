namespace Xpress_backend_V2.Models.DTO
{
    public class AirlineDetailDTO
    {
        public string Name { get; set; }
        public decimal Cost { get; set; } // Use decimal for currency
    }

    public class TravelRequestUploadTicketDTO
    {
        public string TravelAgencyName { get; set; }
        public decimal AgencyBookingCharge { get; set; } // Use decimal
        public decimal TotalExpense { get; set; }      // Use decimal
        public string PdfFilePath { get; set; }         // Cloudinary URL
        public List<AirlineDetailDTO> Airlines { get; set; }
    }
}