using Xpress_backend_V2.Models;

namespace Xpress_backend_V2.Interface
{
    public interface IPassportDocServices
    {
        Task<IEnumerable<PassportDoc>> GetAllAsync();
        Task<PassportDoc> GetByIdAsync(int passportDocId);
        Task AddAsync(PassportDoc passportDoc);
        Task UpdateAsync(PassportDoc passportDoc);
        Task DeleteAsync(int passportDocId);
        Task<IEnumerable<PassportDoc>> GetByUserAsync(int userId);
    }
}
