using AutoMapper;
using Xpress_backend_V2.Models;
using Xpress_backend_V2.Models.DTO;

namespace Xpress_backend_V2.Profiles
{
    public class MappingProfiles : Profile
    {
        public MappingProfiles()
        {
            // --- From DTO to Entity ---

            CreateMap<DocumentDTO, PassportDoc>()
                .ForMember(dest => dest.PassportDocId, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.PassportNumber, opt => opt.MapFrom(src => src.PassportNumber))
                .ForMember(dest => dest.IssueDate, opt => opt.Condition(src => src.PassportIssueDate.HasValue))
                .ForMember(dest => dest.IssueDate, opt => opt.MapFrom(src => src.PassportIssueDate))
                .ForMember(dest => dest.ExpiryDate, opt => opt.Condition(src => src.PassportExpiryDate.HasValue))
                .ForMember(dest => dest.ExpiryDate, opt => opt.MapFrom(src => src.PassportExpiryDate))
                .ForMember(dest => dest.IssuingCountry, opt => opt.MapFrom(src => src.IssuingCountry))
                .ForMember(dest => dest.DocumentPath, opt => opt.MapFrom(src => src.DocumentPath))
                .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
            .ForMember(dest => dest.UploadedAt, opt => opt.Ignore());

            CreateMap<DocumentDTO, VisaDoc>()
                .ForMember(dest => dest.VisaDocId, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.VisaNumber, opt => opt.MapFrom(src => src.VisaNumber))
                .ForMember(dest => dest.IssueDate, opt => opt.Condition(src => src.VisaIssueDate.HasValue))
                .ForMember(dest => dest.IssueDate, opt => opt.MapFrom(src => src.VisaIssueDate))
                .ForMember(dest => dest.IssueDate, opt => opt.Condition(src => src.VisaExpiryDate.HasValue))
                .ForMember(dest => dest.ExpiryDate, opt => opt.MapFrom(src => src.VisaExpiryDate))
                .ForMember(dest => dest.VisaClass, opt => opt.MapFrom(src => src.VisaClass))
                .ForMember(dest => dest.DocumentPath, opt => opt.MapFrom(src => src.DocumentPath))
                .ForMember(dest => dest.UploadedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

            CreateMap<DocumentDTO, AadharDoc>()
                .ForMember(dest => dest.AadharId, opt => opt.Ignore())
                .ForMember(dest => dest.AadharName, opt => opt.MapFrom(src => src.AadharName))
                .ForMember(dest => dest.AadharNumber, opt => opt.MapFrom(src => src.AadharNumber))
                .ForMember(dest => dest.UploadedAt, opt => opt.Ignore())
                .ForMember(dest => dest.DocumentPath, opt => opt.MapFrom(src => src.DocumentPath))
                .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

            // --- From Entity to DTO ---

            CreateMap<PassportDoc, DocumentDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.PassportDocId))
                .ForMember(dest => dest.IDType, opt => opt.MapFrom(src => "Passport"))
                .ForMember(dest => dest.PassportNumber, opt => opt.MapFrom(src => src.PassportNumber))
                .ForMember(dest => dest.PassportIssueDate, opt => opt.MapFrom(src => src.IssueDate))
                .ForMember(dest => dest.PassportExpiryDate, opt => opt.MapFrom(src => src.ExpiryDate))
                .ForMember(dest => dest.IssuingCountry, opt => opt.MapFrom(src => src.IssuingCountry))
                .ForMember(dest => dest.DocumentPath, opt => opt.MapFrom(src => src.DocumentPath))
                .ForMember(dest => dest.UploadDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

            CreateMap<VisaDoc, DocumentDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.VisaDocId))
                .ForMember(dest => dest.IDType, opt => opt.MapFrom(src => "Visa"))
                .ForMember(dest => dest.VisaNumber, opt => opt.MapFrom(src => src.VisaNumber))
                .ForMember(dest => dest.VisaIssueDate, opt => opt.MapFrom(src => src.IssueDate))
                .ForMember(dest => dest.VisaExpiryDate, opt => opt.MapFrom(src => src.ExpiryDate))
                .ForMember(dest => dest.VisaClass, opt => opt.MapFrom(src => src.VisaClass))
                .ForMember(dest => dest.DocumentPath, opt => opt.MapFrom(src => src.DocumentPath))
                .ForMember(dest => dest.UploadDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

            CreateMap<AadharDoc, DocumentDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.AadharId))
                .ForMember(dest => dest.IDType, opt => opt.MapFrom(src => "Aadhar"))
                .ForMember(dest => dest.AadharName, opt => opt.MapFrom(src => src.AadharName))
                .ForMember(dest => dest.AadharNumber, opt => opt.MapFrom(src => src.AadharNumber))
                .ForMember(dest => dest.DocumentPath, opt => opt.MapFrom(src => src.DocumentPath))
                .ForMember(dest => dest.UploadDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));
        }
    }
}
