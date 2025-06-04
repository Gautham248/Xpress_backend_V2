using Xpress_backend_V2.Models;

namespace Xpress_backend_V2.Interface
{
    public interface IAadharDocServices
    {
        Task<IEnumerable<AadharDoc>> GetAllAsync();
        Task<AadharDoc> GetByIdAsync(int aadharId);
        Task AddAsync(AadharDoc aadharDoc);
        Task UpdateAsync(AadharDoc aadharDoc);
        Task DeleteAsync(int aadharId);
        Task<IEnumerable<AadharDoc>> GetByUserAsync(int userId);
    }
}
