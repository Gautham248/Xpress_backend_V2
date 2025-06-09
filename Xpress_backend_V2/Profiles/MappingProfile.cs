using AutoMapper;
using System.Globalization;
using Xpress_backend_V2.Models;
using Xpress_backend_V2.Models.DTO;

namespace Xpress_backend_V2.Profiles
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<TravelRequest, TravelRequestDTO>()
            .ForMember(dest => dest.RequestId, opt => opt.MapFrom(src => src.RequestId))
            .ForMember(dest => dest.SourcePlace, opt => opt.MapFrom(src => src.SourcePlace))
            .ForMember(dest => dest.SourceCountry, opt => opt.MapFrom(src => src.SourceCountry))
            .ForMember(dest => dest.DestinationPlace, opt => opt.MapFrom(src => src.DestinationPlace))
            .ForMember(dest => dest.DestinationCountry, opt => opt.MapFrom(src => src.DestinationCountry))
            .ForMember(dest => dest.OutboundDepartureDate, opt => opt.MapFrom(src => src.OutboundDepartureDate))
            .ForMember(dest => dest.OutboundArrivalDate, opt => opt.MapFrom(src => src.OutboundArrivalDate))
            .ForMember(dest => dest.ReturnDepartureDate, opt => opt.MapFrom(src => src.ReturnDepartureDate))
            .ForMember(dest => dest.ReturnArrivalDate, opt => opt.MapFrom(src => src.ReturnArrivalDate))
            .ForMember(dest => dest.IsAccommodationRequired, opt => opt.MapFrom(src => src.IsAccommodationRequired))
            .ForMember(dest => dest.IsPickupRequired, opt => opt.MapFrom(src => src.IsPickUpRequired))
            .ForMember(dest => dest.IsDropoffRequired, opt => opt.MapFrom(src => src.IsDropOffRequired))
            .ForMember(dest => dest.PickupPlace, opt => opt.MapFrom(src => src.IsPickUpRequired ? src.PickUpPlace : null))
            .ForMember(dest => dest.DropoffPlace, opt => opt.MapFrom(src => src.IsDropOffRequired ? src.DropOffPlace : null))
            .ForMember(dest => dest.Comments, opt => opt.MapFrom(src => src.Comments))
            .ForMember(dest => dest.PurposeOfTravel, opt => opt.MapFrom(src => src.PurposeOfTravel))
            .ForMember(dest => dest.IsVegetarian, opt => opt.MapFrom(src => src.IsVegetarian))
            .ForMember(dest => dest.AttendedCct, opt => opt.MapFrom(src => src.AttendedCCT))
            .ForMember(dest => dest.TravelAgencyName, opt => opt.MapFrom(src => src.TravelAgencyName))
            .ForMember(dest => dest.TotalExpense, opt => opt.MapFrom(src => src.TotalExpense))
            .ForMember(dest => dest.UploadedTicketPdfPath, opt => opt.MapFrom(src => src.TicketDocumentPath))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt))
            .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => src.User != null ? src.User.EmployeeName : "Unknown"))
            .ForMember(dest => dest.IsInternational, opt => opt.MapFrom(src => src.IsInternational))
            .ForMember(dest => dest.IsRoundTrip, opt => opt.MapFrom(src => src.IsRoundTrip))
            .ForMember(dest => dest.ProjectName, opt => opt.MapFrom(src => src.Project != null ? src.Project.ProjectName : "Unknown"))
            .ForMember(dest => dest.TravelModeName, opt => opt.MapFrom(src => src.TravelMode != null ? src.TravelMode.TravelModeName : "Unknown"))
            .ForMember(dest => dest.CurrentStatusName, opt => opt.MapFrom(src => src.CurrentStatus != null ? src.CurrentStatus.StatusName : "Unknown"))
            .ForMember(dest => dest.SelectedTicketOptionId, opt => opt.MapFrom(src => src.SelectedTicketOption != null ? src.SelectedTicketOptionId : null))
            .ForMember(dest => dest.DuId, opt => opt.MapFrom(src => src.Project != null ? src.Project.DuId : 0))
            .ForMember(dest => dest.ProjectManagerName, opt => opt.MapFrom(src => src.Project != null ? src.Project.ProjectManager : "Unknown"));
        
        CreateMap<TravelRequest, TravelRequestDTO>();
            CreateMap<TravelRequestCreateDTO, TravelRequest>()
.ForMember(dest => dest.RequestId, opt => opt.Ignore())
.ForMember(dest => dest.CurrentStatusId, opt => opt.Ignore())
.ForMember(dest => dest.SelectedTicketOptionId, opt => opt.Ignore())
.ForMember(dest => dest.TravelAgencyName, opt => opt.Ignore())
.ForMember(dest => dest.TravelAgencyExpense, opt => opt.Ignore())
.ForMember(dest => dest.BookedAirlines, opt => opt.Ignore())
.ForMember(dest => dest.TotalExpense, opt => opt.Ignore())
.ForMember(dest => dest.TicketDocumentPath, opt => opt.Ignore())
.ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
.ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
.ForMember(dest => dest.IsActive, opt => opt.Ignore());

            CreateMap<TravelRequest, TravelRequestResponseDTO>();

            CreateMap<AuditLog, AuditLogResponseDTO>();
            CreateMap<AuditLogDTO, AuditLog>();

            CreateMap<CreateTicketOptionDTO, TicketOption>();
            CreateMap<UpdateTicketOptionDTO, TicketOption>()
                .ForMember(dest => dest.OptionId, opt => opt.Ignore())
                .ForMember(dest => dest.RequestId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedByUserId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsSelected, opt => opt.Ignore());

            CreateMap<TicketOption, TicketOptionResponseDTO>();


            CreateMap<AuditLog, TimelineEventDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.LogId.ToString()))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src =>
                    src.ActionType == "REQUEST_CREATED" ? "Pending" :
                    src.ActionType == "REQUEST_MODIFIED" ? "Modified" :
                    src.NewStatus != null ? src.NewStatus.StatusName : src.ActionType))
                .ForMember(dest => dest.Date, opt => opt.MapFrom(src =>
                    src.ActionDate == DateTime.MinValue
                        ? src.Timestamp.ToString("dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture)
                        : src.ActionDate.ToString("dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture)))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.ChangeDescription ?? "No description provided."))
                .ForMember(dest => dest.Details, opt => opt.MapFrom(src => src.Comments));
        }
    }
}
