using Xpress_backend_V2.Models.DTO;

namespace Xpress_backend_V2.Interface
{
    public interface IDocumentStatusRepository
    {
        Task<PassportStatusResponseDto> GetPassportStatusAsync(DateTime endDate);
        Task<VisaStatusResponseDto> GetVisaStatusAsync(DateTime endDate);
    }
}
