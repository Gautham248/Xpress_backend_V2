using Microsoft.EntityFrameworkCore;
using Xpress_backend_V2.Data;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models;

namespace Xpress_backend_V2.Repository
{
    public class AadharDocRepository : IAadharDocServices
    {
        private readonly ApiDbContext _context;

        public AadharDocRepository(ApiDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<AadharDoc>> GetAllAsync()
        {
            return await _context.AadharDocs
                .Include(ad => ad.User)
                .Include(ad => ad.CreatedByUser)
                .ToListAsync();
        }

        public async Task<AadharDoc> GetByIdAsync(int aadharId)
        {
            return await _context.AadharDocs
                .Include(ad => ad.User)
                .Include(ad => ad.CreatedByUser)
                .FirstOrDefaultAsync(ad => ad.AadharId == aadharId);
        }

        public async Task AddAsync(AadharDoc aadharDoc)
        {
            aadharDoc.UploadedAt = DateTime.UtcNow;
            _context.AadharDocs.Add(aadharDoc);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(AadharDoc aadharDoc)
        {
            _context.Entry(aadharDoc).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int aadharId)
        {
            var aadharDoc = await _context.AadharDocs.FindAsync(aadharId);
            if (aadharDoc != null)
            {
                _context.AadharDocs.Remove(aadharDoc);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<AadharDoc>> GetByUserAsync(int userId)
        {
            return await _context.AadharDocs
                .Include(ad => ad.User)
                .Include(ad => ad.CreatedByUser)
                .Where(ad => ad.UserId == userId)
                .ToListAsync();
        }
    }
}
