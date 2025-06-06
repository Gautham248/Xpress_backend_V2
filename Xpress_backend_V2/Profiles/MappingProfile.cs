using AutoMapper;
using Xpress_backend_V2.Models;
using Xpress_backend_V2.Models.DTO;

namespace Xpress_backend_V2.Profiles
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<TravelRequestDTO, TravelRequest>();
            CreateMap<TravelRequest, TravelRequestDTO>();
            CreateMap<TravelRequestCreateDTO, TravelRequest>()
.ForMember(dest => dest.RequestId, opt => opt.Ignore())
.ForMember(dest => dest.CurrentStatusId, opt => opt.Ignore())
.ForMember(dest => dest.SelectedTicketOptionId, opt => opt.Ignore())
.ForMember(dest => dest.TravelAgencyName, opt => opt.Ignore())
.ForMember(dest => dest.TravelAgencyExpense, opt => opt.Ignore())
.ForMember(dest => dest.AirlineId, opt => opt.Ignore())
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


            CreateMap<TravelRequest, TravelRequest_EmployeeDashboardDTO>()
           .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.RequestId))
           .ForMember(dest => dest.DepartureDate, opt => opt.MapFrom(src => src.OutboundDepartureDate.ToString("o")))
           .ForMember(dest => dest.ReturnDate, opt => opt.MapFrom(src => src.ReturnArrivalDate.HasValue ? src.ReturnArrivalDate.Value.ToString("o") : null))
           .ForMember(dest => dest.Destination, opt => opt.MapFrom(src => src.DestinationCountry))
           .ForMember(dest => dest.Purpose, opt => opt.MapFrom(src => src.PurposeOfTravel))
           .ForMember(dest => dest.CurrentStatusId, opt => opt.MapFrom(src => src.CurrentStatusId));
            // Status is set in the controller after mapping
        }
    }
}
