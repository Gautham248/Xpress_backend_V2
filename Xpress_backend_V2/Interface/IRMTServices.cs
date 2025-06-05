using Xpress_backend_V2.Models;

namespace Xpress_backend_V2.Interface
{
    public interface IRMTServices
    {
        Task<List<string>> GetAllProjectCodesAsync(); 
        Task<IEnumerable<RMT>> GetAllAsync();
        Task<RMT> GetByIdAsync(int projectId);
        Task<RMT> GetByProjectCodeAsync(string projectCode);
        Task AddAsync(RMT rmt);
        Task UpdateAsync(RMT rmt);
        Task DeleteAsync(int projectId);
    }
}
