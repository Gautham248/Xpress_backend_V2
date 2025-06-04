using AutoMapper;
using Xpress_backend_V2.Models;
using Xpress_backend_V2.Models.DTO;

namespace Xpress_backend_V2.Profiles
{
    public class MappingProfile : Profile
    {
        public MappingProfile() {
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
        }
    }
}
