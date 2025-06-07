using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xpress_backend_V2.Data; // Assuming your DbContext is here
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models;

namespace Xpress_backend_V2.Repositories
{
    public class ProcessingTimeRepository : IProcessingTimeRepository
    {
        private readonly ApiDbContext _context;

        private const string StartStatus = "PendingReview";
        private const string EndStatus = "TicketDispatched";

        public ProcessingTimeRepository(ApiDbContext context)
        {
            _context = context;
        }

        public async Task<(TimeSpan averageTime, int requestCount)> GetAverageProcessingTimeAsync()
        {
            var relevantLogs = _context.AuditLogs
                .Include(log => log.NewStatus)
                .Where(log => log.NewStatus != null &&
                               (log.NewStatus.StatusName == StartStatus || log.NewStatus.StatusName == EndStatus));

            var processingDurations = await relevantLogs
                .GroupBy(log => log.RequestId)
                // START OF THE FIX
                .Select(group => new {
                    // This new structure is translatable to SQL.
                    // It finds the first log with the start status and selects its Timestamp into a nullable DateTime.
                    // If no log is found, FirstOrDefault() returns the default value for a nullable DateTime, which is null.
                    StartTime = group.Where(l => l.NewStatus.StatusName == StartStatus)
                                   .Select(l => (DateTime?)l.Timestamp)
                                   .FirstOrDefault(),

                    // Same logic for the end status.
                    EndTime = group.Where(l => l.NewStatus.StatusName == EndStatus)
                                 .Select(l => (DateTime?)l.Timestamp)
                                 .FirstOrDefault()
                })
                // END OF THE FIX
                .Where(x => x.StartTime.HasValue && x.EndTime.HasValue)
                .Select(x => x.EndTime.Value - x.StartTime.Value)
                .ToListAsync();

            if (processingDurations.Count == 0)
            {
                return (TimeSpan.Zero, 0);
            }

            double averageTicks = processingDurations.Average(ts => ts.Ticks);
            var averageTimeSpan = new TimeSpan((long)averageTicks);

            return (averageTimeSpan, processingDurations.Count);
        }
    }
}