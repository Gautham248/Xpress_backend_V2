namespace Xpress_backend_V2.Models.DTO
{
    public class TravelAgencyStatDto
    {
        public string TravelAgencyName { get; set; }

       
        /// The type of travel, either "Domestic" or "International".
       
        public string TravelType { get; set; }

        
        /// The total number of requests handled by this agency for this travel type.
     
        public int RequestCount { get; set; }

       
        /// The sum of all expenses for this agency for this travel type.
        
        public decimal TotalExpense { get; set; }
    }
}
