using Xpress_backend_V2.Models.DTO;

namespace Xpress_backend_V2.Interface
{
    public interface IAirlineReportRepository
    {
        Task<IEnumerable<AirlineReportDto>> GetAirlineReportAsync(DateTime startDate, DateTime endDate);
    }
}
