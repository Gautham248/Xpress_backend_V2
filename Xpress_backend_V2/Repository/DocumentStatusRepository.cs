// In Repository/DocumentStatusRepository.cs
using Microsoft.EntityFrameworkCore;
using Xpress_backend_V2.Data;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models.DTO;

namespace Xpress_backend_V2.Repository
{
    public class DocumentStatusRepository : IDocumentStatusRepository
    {
        private readonly ApiDbContext _context;

        public DocumentStatusRepository(ApiDbContext context)
        {
            _context = context;
        }

        public async Task<PassportStatusResponseDto> GetPassportStatusAsync(DateTime endDate)
        {
            var date45Days = endDate.AddDays(45);
            var date90Days = endDate.AddDays(90);

            var allRelevantPassports = await _context.PassportDocs
                .Include(p => p.User)
                .Where(p => p.IsActive && p.User.IsActive && p.ExpiryDate < date90Days)
                .ToListAsync();

         
            var expiredCount = allRelevantPassports.Count(p => p.ExpiryDate < endDate);
            var expiresIn45DaysCount = allRelevantPassports.Count(p => p.ExpiryDate >= endDate && p.ExpiryDate <= date45Days);
            
            var expiresIn90DaysCount = allRelevantPassports.Count(p => p.ExpiryDate > date45Days && p.ExpiryDate <= date90Days);
           
            var passportDetails = allRelevantPassports.Select(p => new PassportStatusDto
            {
                EmployeeName = p.User.EmployeeName,
                EmployeeEmail = p.User.EmployeeEmail,
                ExpiryDate = p.ExpiryDate,
                Department = p.User.Department,
                DocStatus = p.ExpiryDate < endDate ? "Expired" : "Not Expired"
            }).ToList();

            return new PassportStatusResponseDto
            {
                PassportDetails = passportDetails,
                ExpiredCount = expiredCount,
                ExpiresIn45DaysCount = expiresIn45DaysCount,
                ExpiresIn90DaysCount = expiresIn90DaysCount
            };
        }

        public async Task<VisaStatusResponseDto> GetVisaStatusAsync(DateTime endDate)
        {
            var date45Days = endDate.AddDays(45);
            var date90Days = endDate.AddDays(90);

            var allRelevantVisas = await _context.VisaDocs
                .Include(v => v.User)
                .Where(v => v.IsActive && v.User.IsActive && v.ExpiryDate < date90Days)
                .ToListAsync();

            
            var expiredCount = allRelevantVisas.Count(v => v.ExpiryDate < endDate);
            var expiresIn45DaysCount = allRelevantVisas.Count(v => v.ExpiryDate >= endDate && v.ExpiryDate <= date45Days);
           
            var expiresIn90DaysCount = allRelevantVisas.Count(v => v.ExpiryDate > date45Days && v.ExpiryDate <= date90Days);
          

            var visaDetails = allRelevantVisas.Select(v => new VisaStatusDto
            {
                EmployeeName = v.User.EmployeeName,
                EmployeeEmail = v.User.EmployeeEmail,
                ExpiryDate = v.ExpiryDate,
                Department = v.User.Department,
                DocStatus = v.ExpiryDate < endDate ? "Expired" : "Not Expired"
            }).ToList();

            return new VisaStatusResponseDto
            {
                VisaDetails = visaDetails,
                ExpiredCount = expiredCount,
                ExpiresIn45DaysCount = expiresIn45DaysCount,
                ExpiresIn90DaysCount = expiresIn90DaysCount
            };
        }
    }
}