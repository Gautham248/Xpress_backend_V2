using Microsoft.EntityFrameworkCore;
using Xpress_backend_V2.Data;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models;

namespace Xpress_backend_V2.Repository
{
    public class UserNotificationRepository : IUserNotificationServices
    {
        private readonly ApiDbContext _context;

        public UserNotificationRepository(ApiDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<UserNotification>> GetAllAsync()
        {
            return await _context.UserNotifications
                .Include(un => un.User)
                .Include(un => un.Notification)
                .ToListAsync();
        }

        public async Task<UserNotification> GetByIdAsync(int userNotificationId)
        {
            return await _context.UserNotifications
                .Include(un => un.User)
                .Include(un => un.Notification)
                .FirstOrDefaultAsync(un => un.UserNotificationId == userNotificationId);
        }

        public async Task AddAsync(UserNotification userNotification)
        {
            _context.UserNotifications.Add(userNotification);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(UserNotification userNotification)
        {
            _context.Entry(userNotification).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int userNotificationId)
        {
            var userNotification = await _context.UserNotifications.FindAsync(userNotificationId);
            if (userNotification != null)
            {
                _context.UserNotifications.Remove(userNotification);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<UserNotification>> GetByUserAsync(int userId)
        {
            return await _context.UserNotifications
                .Include(un => un.User)
                .Include(un => un.Notification)
                .Where(un => un.UserId == userId)
                .ToListAsync();
        }
    }
}
