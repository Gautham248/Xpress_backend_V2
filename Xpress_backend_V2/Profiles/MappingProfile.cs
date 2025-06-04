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
        }
    }
}
