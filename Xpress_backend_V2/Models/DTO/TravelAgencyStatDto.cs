namespace Xpress_backend_V2.Models.DTO
{
    public class TravelAgencyStatDto
    {
        public string TravelAgencyName { get; set; }

        /// <summary>
        /// The type of travel, either "Domestic" or "International".
        /// </summary>
        public string TravelType { get; set; }

        /// <summary>
        /// The total number of requests handled by this agency for this travel type.
        /// </summary>
        public int RequestCount { get; set; }

        /// <summary>
        /// The sum of all expenses for this agency for this travel type.
        /// </summary>
        public decimal TotalExpense { get; set; }
    }
}
