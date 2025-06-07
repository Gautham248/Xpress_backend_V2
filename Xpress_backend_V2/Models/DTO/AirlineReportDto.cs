namespace Xpress_backend_V2.Models.DTO
{
    public class AirlineReportDto
    {
        public string AirlineName { get; set; }

        /// <summary>
        /// Type of travel, either "Domestic" or "International".
        /// </summary>
        public string TypeOfTravel { get; set; }

        /// <summary>
        /// The count of travel requests for this airline and travel type.
        /// </summary>
        public int TravelRequestCount { get; set; }

        /// <summary>
        /// The sum of all expenses for this airline and travel type.
        /// </summary>
        public decimal TotalAirlineExpense { get; set; }
    }
}

