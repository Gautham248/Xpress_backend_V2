using Xpress_backend_V2.Models;

namespace Xpress_backend_V2.Interface
{
    public interface IUserNotificationServices
    {
        Task<IEnumerable<UserNotification>> GetAllAsync();
        Task<UserNotification> GetByIdAsync(int userNotificationId);
        Task AddAsync(UserNotification userNotification);
        Task UpdateAsync(UserNotification userNotification);
        Task DeleteAsync(int userNotificationId);
        Task<IEnumerable<UserNotification>> GetByUserAsync(int userId);
    }
}
