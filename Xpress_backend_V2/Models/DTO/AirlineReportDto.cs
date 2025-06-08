namespace Xpress_backend_V2.Models.DTO
{
    public class AirlineReportDto
    {
        public string AirlineName { get; set; }

        
        /// Type of travel, either "Domestic" or "International".
        
        public string TypeOfTravel { get; set; }

        
        /// The count of travel requests for this airline and travel type.
       
        public int TravelRequestCount { get; set; }

      
        /// The sum of all expenses for this airline and travel type.
      
        public decimal TotalAirlineExpense { get; set; }
    }
}

