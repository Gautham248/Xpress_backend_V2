using Xpress_backend_V2.Models;

namespace Xpress_backend_V2.Interface
{
    public interface ITokenService
    {
        Task<string> GenerateActionTokenAsync(string requestId, string userEmail, string actionType, int? optionId = null, TimeSpan? expiryDuration = null);
        Task<EmailActionToken?> ValidateAndConsumeTokenAsync(string tokenValue);
        Task<EmailActionToken?> PeekTokenAsync(string tokenValue);
    }
}
