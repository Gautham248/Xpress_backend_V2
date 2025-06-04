using Xpress_backend_V2.Models;

namespace Xpress_backend_V2.Interface
{
    public interface INotificationServices
    {
        Task<IEnumerable<Notification>> GetAllAsync();
        Task<Notification> GetByIdAsync(int notificationId);
        Task AddAsync(Notification notification);
        Task UpdateAsync(Notification notification);
        Task DeleteAsync(int notificationId);
        Task<IEnumerable<Notification>> GetByUserAsync(int userId);
    }
}
