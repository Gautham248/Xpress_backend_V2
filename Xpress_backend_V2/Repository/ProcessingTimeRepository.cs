using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xpress_backend_V2.Data;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models.DTO;

namespace Xpress_backend_V2.Repositories
{

    public class ProcessingTimeRepository : IProcessingTimeRepository
    {
        private readonly ApiDbContext _context;

       
        private const string StartStatusName = "PendingReview";
        private const string EndStatusName = "TicketDispatched";

        public ProcessingTimeRepository(ApiDbContext context)
        {
            _context = context;
        }

        public async Task<AverageProcessingTimeDto?> GetAverageReviewToDispatchTimeAsync()
        {
           
            var startStatusId = await _context.RequestStatuses
                .Where(s => s.StatusName == StartStatusName)
                .Select(s => s.StatusId)
                .FirstOrDefaultAsync();

            var endStatusId = await _context.RequestStatuses
                .Where(s => s.StatusName == EndStatusName)
                .Select(s => s.StatusId)
                .FirstOrDefaultAsync();

          
            if (startStatusId == 0 || endStatusId == 0)
            {
                return null; 
            }

          
            var requestDurations = await _context.AuditLogs
               
                .Where(log => log.NewStatusId == startStatusId || log.NewStatusId == endStatusId)
                // Group all relevant logs by their RequestId
                .GroupBy(log => log.RequestId)
           
                .Select(group => new
                {
                    //  earliest timestamp for the 'PendingReview' status in the group
                    StartTime = group
                        .Where(g => g.NewStatusId == startStatusId)
                        .OrderBy(g => g.Timestamp) // Important: get the first occurrence
                        .Select(g => (DateTime?)g.Timestamp) // Cast to nullable DateTime
                        .FirstOrDefault(),

                    //  earliest timestamp for the 'TicketDispatched' status in the group
                    EndTime = group
                        .Where(g => g.NewStatusId == endStatusId)
                        .OrderBy(g => g.Timestamp) // Important: get the first occurrence
                        .Select(g => (DateTime?)g.Timestamp)
                        .FirstOrDefault()
                })
                
                .Where(x => x.StartTime.HasValue && x.EndTime.HasValue && x.EndTime.Value > x.StartTime.Value)
              
                .Select(x => x.EndTime.Value - x.StartTime.Value)
                .ToListAsync();

         
            if (requestDurations == null || !requestDurations.Any())
            {
              
                return new AverageProcessingTimeDto
                {
                    AverageDays = 0,
                    AverageHours = 0,
                    AverageMinutes = 0,
                    ReadableFormat = "N/A - No completed requests found.",
                    TotalRequestsCalculated = 0
                };
            }

           
            var averageTicks = requestDurations.Average(span => span.Ticks);
            var averageTimeSpan = TimeSpan.FromTicks((long)averageTicks);

           
            return new AverageProcessingTimeDto
            {
                AverageDays = averageTimeSpan.TotalDays,
                AverageHours = averageTimeSpan.TotalHours,
                AverageMinutes = averageTimeSpan.TotalMinutes,
                ReadableFormat = $"{averageTimeSpan.Days} Days, {averageTimeSpan.Hours} Hours, {averageTimeSpan.Minutes} Minutes",
                TotalRequestsCalculated = requestDurations.Count
            };
        }
    }
}