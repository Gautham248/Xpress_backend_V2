using Microsoft.EntityFrameworkCore;
using Xpress_backend_V2.Data;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models;

namespace Xpress_backend_V2.Repository
{
    public class NotificationRepository : INotificationServices
    {
        private readonly ApiDbContext _context;

        public NotificationRepository(ApiDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Notification>> GetAllAsync()
        {
            return await _context.Notifications
                .Include(n => n.CreatedByUser)
                .Include(n => n.UserNotifications)
                .ToListAsync();
        }

        public async Task<Notification> GetByIdAsync(int notificationId)
        {
            return await _context.Notifications
                .Include(n => n.CreatedByUser)
                .Include(n => n.UserNotifications)
                .FirstOrDefaultAsync(n => n.NotificationId == notificationId);
        }

        public async Task AddAsync(Notification notification)
        {
            notification.CreatedAt = DateTime.UtcNow;
            notification.UpdatedAt = DateTime.UtcNow;
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Notification notification)
        {
            notification.UpdatedAt = DateTime.UtcNow;
            _context.Entry(notification).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Notification>> GetByUserAsync(int userId)
        {
            return await _context.Notifications
                .Include(n => n.CreatedByUser)
                .Include(n => n.UserNotifications)
                .Where(n => n.CreatedBy == userId)
                .ToListAsync();
        }
    }
}
