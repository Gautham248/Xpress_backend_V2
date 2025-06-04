using Microsoft.EntityFrameworkCore;
using Xpress_backend_V2.Data;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models;

namespace Xpress_backend_V2.Repository
{
    public class TicketOptionRepository : ITicketOptionServices
    {
        private readonly ApiDbContext _context;

        public TicketOptionRepository(ApiDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<TicketOption>> GetAllAsync()
        {
            return await _context.TicketOptions
                .Include(to => to.TravelRequest)
                .Include(to => to.CreatedByUser)
                .ToListAsync();
        }

        public async Task<TicketOption> GetByIdAsync(int optionId)
        {
            return await _context.TicketOptions
                .Include(to => to.TravelRequest)
                .Include(to => to.CreatedByUser)
                .FirstOrDefaultAsync(op => op.OptionId == optionId);
        }

        public async Task AddAsync(TicketOption ticketOption)
        {
            ticketOption.CreatedAt = DateTime.UtcNow;
            _context.TicketOptions.Add(ticketOption);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(TicketOption ticketOption)
        {
            _context.Entry(ticketOption).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int optionId)
        {
            var ticketOption = await _context.TicketOptions.FindAsync(optionId);
            if (ticketOption != null)
            {
                _context.TicketOptions.Remove(ticketOption);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<TicketOption>> GetByTravelRequestAsync(string requestId)
        {
            return await _context.TicketOptions
                .Include(to => to.TravelRequest)
                .Include(to => to.CreatedByUser)
                .Where(to => to.RequestId == requestId)
                .ToListAsync();
        }
    }
}
