namespace Xpress_backend_V2.Services.Interface
{
    public interface IAuthService
    {
        public string GenerateToken(int userId, string email, string role);
    }
}
