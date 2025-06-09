namespace Xpress_backend_V2.Models
{
    public class TravelRequest
    {
        public string RequestId { get; set; } // PK
        public int UserId { get; set; } // FK → Users
        public int TravelModeId { get; set; } // FK → TravelModes
        public bool IsInternational { get; set; }
        public bool IsRoundTrip { get; set; }
        public string ProjectCode { get; set; } // FK → RMT
        public string SourcePlace { get; set; }
        public string SourceCountry { get; set; }
        public string DestinationPlace { get; set; }
        public string DestinationCountry { get; set; }
        public DateTime OutboundDepartureDate { get; set; } // Always UTC
        public DateTime? OutboundArrivalDate { get; set; } // Always UTC
        public DateTime? ReturnDepartureDate { get; set; } // Always UTC, nullable
        public DateTime? ReturnArrivalDate { get; set; } // Always UTC, nullable
        public bool IsAccommodationRequired { get; set; }
        public bool IsDropOffRequired { get; set; }

        public string? DropOffPlace { get; set; }

        public bool IsPickUpRequired { get; set; }

        public string? PickUpPlace { get; set; }
        public string? Comments { get; set; }
        public string PurposeOfTravel { get; set; }
        public bool IsVegetarian { get; set; }
        public string? FoodComment { get; set; }
        public bool AttendedCCT { get; set; }
        public int CurrentStatusId { get; set; } // FK → RequestStatuses
        public int? SelectedTicketOptionId { get; set; } // FK → TicketOptions
        public string? TravelAgencyName { get; set; }
        public decimal? TravelAgencyExpense { get; set; }
        //public int? AirlineId { get; set; } // FK → Airlines
        public decimal? TotalExpense { get; set; }
        public string? TicketDocumentPath { get; set; }
        public string? LDCertificatePath { get; set; }
        public string? TravelFeedback { get; set; }
        public DateTime CreatedAt { get; set; } // Always UTC
        public DateTime UpdatedAt { get; set; } // Always UTC
        public bool IsActive { get; set; }

        // Navigation properties
        public User User { get; set; }
        public TravelMode TravelMode { get; set; }
        public RMT Project { get; set; }
        public RequestStatus CurrentStatus { get; set; }
        public TicketOption? SelectedTicketOption { get; set; }
        //public Airline? Airline { get; set; }
        public ICollection<Airline> BookedAirlines { get; set; }
        public ICollection<TicketOption> TicketOptions { get; set; }
        public ICollection<AuditLog> AuditLogs { get; set; }
    }
}
