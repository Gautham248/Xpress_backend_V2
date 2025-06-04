using Microsoft.EntityFrameworkCore;
using Xpress_backend_V2.Data;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models;

namespace Xpress_backend_V2.Repository
{
    public class VisaDocRepository : IVisaDocServices
    {
        private readonly ApiDbContext _context;

        public VisaDocRepository(ApiDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<VisaDoc>> GetAllAsync()
        {
            return await _context.VisaDocs
                .Include(vd => vd.User)
                .Include(vd => vd.CreatedByUser)
                .Where(vd => vd.IsActive)
                .ToListAsync();
        }

        public async Task<VisaDoc> GetByIdAsync(int visaDocId)
        {
            return await _context.VisaDocs
                .Include(vd => vd.User)
                .Include(vd => vd.CreatedByUser)
                .FirstOrDefaultAsync(vd => vd.VisaDocId == visaDocId && vd.IsActive);
        }

        public async Task AddAsync(VisaDoc visaDoc)
        {
            visaDoc.UploadedAt = DateTime.UtcNow;
            visaDoc.IsActive = true;
            _context.VisaDocs.Add(visaDoc);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(VisaDoc visaDoc)
        {
            _context.Entry(visaDoc).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int visaDocId)
        {
            var visaDoc = await _context.VisaDocs.FindAsync(visaDocId);
            if (visaDoc != null)
            {
                visaDoc.IsActive = false;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<VisaDoc>> GetByUserAsync(int userId)
        {
            return await _context.VisaDocs
                .Include(vd => vd.User)
                .Include(vd => vd.CreatedByUser)
                .Where(vd => vd.UserId == userId && vd.IsActive)
                .ToListAsync();
        }
    }
}
