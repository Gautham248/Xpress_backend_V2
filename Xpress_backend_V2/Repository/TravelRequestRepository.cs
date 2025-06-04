using Microsoft.EntityFrameworkCore;
using Xpress_backend_V2.Data;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models;
using Xpress_backend_V2.Models.DTO;

namespace Xpress_backend_V2.Repository
{
    public class TravelRequestRepository : ITravelRequestServices
    {
        private readonly ApiDbContext _context;

        public TravelRequestRepository(ApiDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<TravelRequest>> GetAllAsync()
        {
            return await _context.TravelRequests
                .Include(tr => tr.User)
                .Include(tr => tr.TravelMode)
                .Include(tr => tr.Project)
                .Include(tr => tr.CurrentStatus)
                .Include(tr => tr.SelectedTicketOption)
                .Include(tr => tr.Airline)
                .Where(tr => tr.IsActive)
                .ToListAsync();
        }

        public async Task<TravelRequest> GetByIdAsync(string requestId)
        {
            return await _context.TravelRequests
                .Include(tr => tr.User)
                .Include(tr => tr.TravelMode)
                .Include(tr => tr.Project)
                .Include(tr => tr.CurrentStatus)
                .Include(tr => tr.SelectedTicketOption)
                .Include(tr => tr.Airline)
                .FirstOrDefaultAsync(tr => tr.RequestId == requestId && tr.IsActive);
        }

        public async Task AddAsync(TravelRequest travelRequest)
        {
            travelRequest.CreatedAt = DateTime.UtcNow;
            travelRequest.UpdatedAt = DateTime.UtcNow;
            travelRequest.IsActive = true;
            _context.TravelRequests.Add(travelRequest);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(TravelRequest travelRequest)
        {
            travelRequest.UpdatedAt = DateTime.UtcNow;
            _context.Entry(travelRequest).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(string requestId)
        {
            var travelRequest = await _context.TravelRequests.FindAsync(requestId);
            if (travelRequest != null)
            {
                travelRequest.IsActive = false;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<TravelRequest>> GetByStatusAsync(int statusId)
        {
            return await _context.TravelRequests
                .Include(tr => tr.User)
                .Include(tr => tr.TravelMode)
                .Include(tr => tr.Project)
                .Include(tr => tr.CurrentStatus)
                .Where(tr => tr.CurrentStatusId == statusId && tr.IsActive)
                .ToListAsync();
        }

        public async Task<IEnumerable<TravelRequest>> GetByUserAsync(int userId)
        {
            return await _context.TravelRequests
                .Include(tr => tr.User)
                .Include(tr => tr.TravelMode)
                .Include(tr => tr.Project)
                .Include(tr => tr.CurrentStatus)
                .Where(tr => tr.UserId == userId && tr.IsActive)
                .ToListAsync();
        }

        // Travel Info Banner
        public async Task<List<TravelInfoBannerDTO>> GetTravelInfoBannerDetailsAsync(string requestId)
        {
            var query = from tr in _context.TravelRequests
                        join user in _context.Users on tr.UserId equals user.UserId
                        join rmt in _context.RMTs on tr.ProjectCode equals rmt.ProjectCode
                        join mode in _context.TravelModes on tr.TravelModeId equals mode.TravelModeId
                        where tr.RequestId == requestId
                        select new TravelInfoBannerDTO
                        {
                            RequestId = tr.RequestId,
                            EmployeeName = user.EmployeeName,
                            DepartmentName = user.Department,
                            ProjectCode = rmt.ProjectCode,
                            TravelModeName = mode.TravelModeName,
                            SourcePlace = tr.SourcePlace,
                            SourceCountry = tr.SourceCountry,
                            DestinationPlace = tr.DestinationPlace,
                            DestinationCountry = tr.DestinationCountry
                        };

            return await query.ToListAsync();
        }

        //public async Task<IEnumerable<TravelRequest>> GetAllTravelRequestsAsync()
        //{
        //    return await _context.TravelRequests
        //        .Include(tr => tr.User)
        //        .Include(tr => tr.IsInternational)
        //        .Include(tr => tr.IsRoundTrip)
        //        .Include(tr => tr.Project)
        //        .Include(tr => tr.TravelMode)
        //        .Include(tr => tr.CurrentStatus)
        //        .Include(tr => tr.SelectedTicketOption)
        //        .ToListAsync();
        //}

    }
}
