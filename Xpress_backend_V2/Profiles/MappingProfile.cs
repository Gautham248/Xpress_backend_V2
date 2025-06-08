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
            CreateMap<TravelRequestDTO, TravelRequest>();
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
