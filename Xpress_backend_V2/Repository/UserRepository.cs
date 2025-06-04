using Microsoft.EntityFrameworkCore;
using Xpress_backend_V2.Data;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models;

namespace Xpress_backend_V2.Repository
{
    public class UserRepository : IUserServices
    {
        private readonly ApiDbContext _context;

        public UserRepository(ApiDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _context.Users
                .Include(u => u.TravelRequests)
                .Include(u => u.CreatedTicketOptions)
                .Where(u => u.IsActive)
                .ToListAsync();
        }

        public async Task<User> GetByIdAsync(int userId)
        {
            return await _context.Users
                .Include(u => u.TravelRequests)
                .Include(u => u.CreatedTicketOptions)
                .FirstOrDefaultAsync(u => u.UserId == userId && u.IsActive);
        }

        public async Task AddAsync(User user)
        {
            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            user.IsActive = true;
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(User user)
        {
            user.UpdatedAt = DateTime.UtcNow;
            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.IsActive = false;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<User> GetByEmployeeNameAsync(string employeeName)
        {
            return await _context.Users
                .Include(u => u.TravelRequests)
                .Include(u => u.CreatedTicketOptions)
                .FirstOrDefaultAsync(u => u.EmployeeName == employeeName && u.IsActive);
        }
    }
}
