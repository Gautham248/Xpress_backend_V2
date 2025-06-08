namespace Xpress_backend_V2.Models
{
    public class Airline
    {
        public int AirlineId { get; set; } // PK
        public string AirlineName { get; set; }

        public double AirlineExpense { get; set; } // Expense per ticket

        // Navigation property
        public ICollection<TravelRequest> TravelRequests { get; set; }
    }
}
