namespace Xpress_backend_V2.Models.DTO
{
    public class AverageProcessingTimeDto
    {
        /// <summary>
        /// The calculated average time as a TimeSpan object.
        /// Useful for machine-to-machine communication.
        /// </summary>
        public TimeSpan AverageTime { get; set; }

        /// <summary>
        /// A human-readable, formatted string of the average time.
        /// e.g., "2 Days, 5 Hours, 32 Minutes"
        /// </summary>
        public string FormattedAverageTime { get; set; }

        /// <summary>
        /// The total number of requests included in the calculation.
        /// </summary>
        public int TotalRequestsCalculated { get; set; }
    }
}

