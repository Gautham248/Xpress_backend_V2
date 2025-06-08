namespace Xpress_backend_V2.Models.DTO
{
    public class AverageProcessingTimeDto
    {
       
        public double AverageDays { get; set; }

        /// <summary>
        /// The average time in total hours.
        /// </summary>
        public double AverageHours { get; set; }

        /// <summary>
        /// The average time in total minutes.
        /// </summary>
        public double AverageMinutes { get; set; }

        /// <summary>
        /// A human-readable formatted string representing the average time.
        /// Example: "3 Days, 4 Hours, 32 Minutes"
        /// </summary>
        public string ReadableFormat { get; set; }

        /// <summary>
        /// The number of requests included in this calculation.
        /// </summary>
        public int TotalRequestsCalculated { get; set; }

    }
}

