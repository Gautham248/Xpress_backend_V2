using Microsoft.EntityFrameworkCore;
using Xpress_backend_V2.Data;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models;

namespace Xpress_backend_V2.Services
{
    public class ProjectRoleService : IProjectRoleService
    {
        private readonly ApiDbContext _dbContext;

        public ProjectRoleService(ApiDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<RMT>> GetProjectsForEmailAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                throw new ArgumentException("Email cannot be null or empty.", nameof(email));
            }

            // Define "active" statuses (adjust these based on your RequestStatus table)
            var activeStatuses = new List<string> { "PendingReview", "Approved", "InProgress" }; // Example statuses

            var projects = await _dbContext.RMTs
                .Where(rmt =>
                    // Match email with ProjectManagerEmail or DuHeadEmail
                    (rmt.ProjectManagerEmail == email || rmt.DuHeadEmail == email) &&
                    // Ensure there are active travel requests for the project
                    _dbContext.TravelRequests
                        .Where(tr => tr.ProjectCode == rmt.ProjectCode)
                        .Join(_dbContext.RequestStatuses,
                              tr => tr.CurrentStatusId,
                              rs => rs.StatusId,
                              (tr, rs) => new { tr, rs })
                        .Any(joined => activeStatuses.Contains(joined.rs.StatusName)))
                .ToListAsync();

            return projects;
        }
    }
}
