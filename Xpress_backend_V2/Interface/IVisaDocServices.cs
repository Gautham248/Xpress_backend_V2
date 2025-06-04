using Xpress_backend_V2.Models;

namespace Xpress_backend_V2.Interface
{
    public interface IVisaDocServices
    {
        Task<IEnumerable<VisaDoc>> GetAllAsync();
        Task<VisaDoc> GetByIdAsync(int visaDocId);
        Task AddAsync(VisaDoc visaDoc);
        Task UpdateAsync(VisaDoc visaDoc);
        Task DeleteAsync(int visaDocId);
        Task<IEnumerable<VisaDoc>> GetByUserAsync(int userId);
    }
}
