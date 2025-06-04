using Xpress_backend_V2.Models;

namespace Xpress_backend_V2.Interface
{
    public interface IRequestStatusServices
    {
        Task<IEnumerable<RequestStatus>> GetAllAsync();
        Task<RequestStatus> GetByIdAsync(int statusId);
        Task AddAsync(RequestStatus requestStatus);
        Task UpdateAsync(RequestStatus requestStatus);
        Task DeleteAsync(int statusId);
    }
}
