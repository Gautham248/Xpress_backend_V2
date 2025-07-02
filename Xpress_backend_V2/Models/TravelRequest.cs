using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Xpress_backend_V2.Models
{
    public class TravelRequest
    {
        public string RequestId { get; set; } = string.Empty; // PK
        public int UserId { get; set; } // FK → Users
        public int TravelModeId { get; set; } // FK → TravelModes
        public bool IsInternational { get; set; }
        public bool IsRoundTrip { get; set; }
        public string ProjectCode { get; set; } = string.Empty; // FK → RMT
        public string SourcePlace { get; set; } = string.Empty;
        public string SourceCountry { get; set; } = string.Empty;
        public string DestinationPlace { get; set; } = string.Empty;
        public string DestinationCountry { get; set; } = string.Empty;
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
        public string PurposeOfTravel { get; set; } = string.Empty;
        public bool IsVegetarian { get; set; }
        public string? FoodComment { get; set; }
        public bool AttendedCCT { get; set; }
        public int CurrentStatusId { get; set; } // FK → RequestStatuses
        public int? SelectedTicketOptionId { get; set; } // FK → TicketOptions
        public string? TravelAgencyName { get; set; }
        public decimal? TravelAgencyExpense { get; set; }
        public decimal? TotalExpense { get; set; }
        public List<string>? TicketDocumentPath { get; set; }
        [Column(TypeName = "jsonb")]
        public List<string>? AccommodationDocumentPath { get; set; }
        [Column(TypeName = "jsonb")]
        public List<string>? InsuranceDocumentPath { get; set; }
        public string? LDCertificatePath { get; set; }
        public string? TravelFeedback { get; set; }
        public DateTime CreatedAt { get; set; } // Always UTC
        public DateTime UpdatedAt { get; set; } // Always UTC
        public bool IsActive { get; set; }
        public int? AssignedWorkflowId { get; set; } // FK → WorkflowTemplates
        public string? ReportingManagerEmail { get; set; } // Stores email from API

        // Navigation properties
        public User User { get; set; } = null!;
        public TravelMode TravelMode { get; set; } = null!;
        public RMT Project { get; set; } = null!;
        public RequestStatus CurrentStatus { get; set; } = null!;
        public TicketOption? SelectedTicketOption { get; set; }
        public ICollection<Airline> BookedAirlines { get; set; } = new List<Airline>();
        public ICollection<TicketOption> TicketOptions { get; set; } = new List<TicketOption>();
        public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
        public WorkflowTemplate? WorkflowTemplate { get; set; }
        public ICollection<WorkflowHistory> WorkflowHistories { get; set; } = new List<WorkflowHistory>();
    }
}