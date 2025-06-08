namespace Xpress_backend_V2.Models.DTO
{
    public class DashboardDtos
    {
        public class RequestStatusOverviewDto
        {
            public List<RequestStatusItemDto> Requests { get; set; } = new List<RequestStatusItemDto>();
            public int TotalRequestCount { get; set; }
            public int RejectedCount { get; set; }

            // This count includes Confirmed, InTransit, Returned, and Closed statuses
            public int ConfirmedOrOtherCount { get; set; }
        }

        /// <summary>
        /// Represents a single travel request item for the status overview.
        /// </summary>
        public class RequestStatusItemDto
        {
            public string ID { get; set; }
            public DateTime RequestDate { get; set; }
            public string Status { get; set; }
            public string TravelType { get; set; } // "Domestic" or "International"
        }



        /// <summary>
        /// Contains the complete response for the expense overview API.
        /// </summary>
        public class ExpenseOverviewDto
        {
            public List<RequestExpenseItemDto> Requests { get; set; } = new List<RequestExpenseItemDto>();
            public decimal TotalExpense { get; set; }
            public decimal DomesticExpense { get; set; }
            public decimal InternationalExpense { get; set; }
        }

        /// <summary>
        /// Represents a single travel request item for the expense overview.
        /// </summary>
        public class RequestExpenseItemDto
        {
            public string ID { get; set; }
            public DateTime RequestDate { get; set; }
            public string Status { get; set; }
            public string TravelType { get; set; } // "Domestic" or "International"
            public decimal? EstimatedCost { get; set; }
        }


        /// <summary>
        /// Contains the complete response for the trip details overview API.
        /// </summary>
        public class TripDetailsOverviewDto
        {
            public List<TripDetailItemDto> Trips { get; set; } = new List<TripDetailItemDto>();
            public int TotalTripCount { get; set; }
            public int DomesticTripCount { get; set; }
            public int InternationalTripCount { get; set; }
        }

        /// <summary>
        /// Represents a single travel trip item for the details overview.
        /// </summary>
        public class TripDetailItemDto
        {
            public string ID { get; set; }
            public DateTime RequestDate { get; set; }
            public string Status { get; set; }
            public string TravelType { get; set; } // "Domestic" or "International"
            public string? Airline { get; set; }
            public string? TravelAgency { get; set; }
        }
    }
}
