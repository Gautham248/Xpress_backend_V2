using Xpress_backend_V2.Models;

namespace Xpress_backend_V2.Interface
{
    public interface IProjectRoleService
    {
        Task<List<RMT>> GetProjectsForEmailAsync(string email);
    }
}
