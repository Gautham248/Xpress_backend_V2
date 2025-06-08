namespace Xpress_backend_V2.Models
{
    public class Airline
    {
        public int AirlineId { get; set; } // PK
        public string AirlineName { get; set; }

        public double AirlineExpense { get; set; } // Expense per ticket

        public string? RequestId { get; set; } // FK -> Travel Request
        public TravelRequest? TravelRequest { get; set; } // Navigation property
    }
}
