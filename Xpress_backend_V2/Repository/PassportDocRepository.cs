using Microsoft.EntityFrameworkCore;
using Xpress_backend_V2.Data;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models;

namespace Xpress_backend_V2.Repository
{
    public class PassportDocRepository : IPassportDocServices
    {
        private readonly ApiDbContext _context;

        public PassportDocRepository(ApiDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<PassportDoc>> GetAllAsync()
        {
            return await _context.PassportDocs
                .Include(pd => pd.User)
                .Include(pd => pd.CreatedByUser)
                .Where(pd => pd.IsActive)
                .ToListAsync();
        }

        public async Task<PassportDoc> GetByIdAsync(int passportDocId)
        {
            return await _context.PassportDocs
                .Include(pd => pd.User)
                .Include(pd => pd.CreatedByUser)
                .FirstOrDefaultAsync(pd => pd.PassportDocId == passportDocId && pd.IsActive);
        }

        public async Task AddAsync(PassportDoc passportDoc)
        {
            passportDoc.UploadedAt = DateTime.UtcNow;
            passportDoc.IsActive = true;
            _context.PassportDocs.Add(passportDoc);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(PassportDoc passportDoc)
        {
            _context.Entry(passportDoc).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int passportDocId)
        {
            var passportDoc = await _context.PassportDocs.FindAsync(passportDocId);
            if (passportDoc != null)
            {
                passportDoc.IsActive = false;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<PassportDoc>> GetByUserAsync(int userId)
        {
            return await _context.PassportDocs
                .Include(pd => pd.User)
                .Include(pd => pd.CreatedByUser)
                .Where(pd => pd.UserId == userId && pd.IsActive)
                .ToListAsync();
        }
    }
}
