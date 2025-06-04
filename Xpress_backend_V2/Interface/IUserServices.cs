using Xpress_backend_V2.Models;

namespace Xpress_backend_V2.Interface
{
    public interface IUserServices
    {
        Task<IEnumerable<User>> GetAllAsync();
        Task<User> GetByIdAsync(int userId);
        Task AddAsync(User user);
        Task UpdateAsync(User user);
        Task DeleteAsync(int userId);
        Task<User> GetByEmployeeNameAsync(string employeeName);
    }
}
