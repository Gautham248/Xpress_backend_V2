using Xpress_backend_V2.Models;

namespace Xpress_backend_V2.Interface
{
    public interface ITicketOptionServices
    {
        Task<IEnumerable<TicketOption>> GetAllAsync();
        Task<TicketOption> GetByIdAsync(int optionId);
        Task AddAsync(TicketOption ticketOption);
        Task UpdateAsync(TicketOption ticketOption);
        Task DeleteAsync(int optionId);
        Task<IEnumerable<TicketOption>> GetByTravelRequestAsync(string requestId);
    }
}
