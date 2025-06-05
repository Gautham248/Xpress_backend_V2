using Xpress_backend_V2.Models;
using Xpress_backend_V2.Models.DTO;

namespace Xpress_backend_V2.Interface
{
    public interface IUserRepository
    {
        Task<User> RegisterUser(UserRegisterDTO user);
        Task<User> LoginUser(String email, String password);
    }
}
