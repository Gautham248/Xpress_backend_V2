using Microsoft.EntityFrameworkCore;
using Xpress_backend_V2.Data;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models;

namespace Xpress_backend_V2.Repository
{
    public class TokenService : ITokenService
    {
        private readonly ApiDbContext _context;
        private readonly ILogger<TokenService> _logger;

        public TokenService(ApiDbContext context, ILogger<TokenService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<string> GenerateActionTokenAsync(string requestId, string userEmail, string actionType, int? optionId = null, TimeSpan? expiryDuration = null)
        {
            expiryDuration ??= TimeSpan.FromHours(24); // Default expiry

            var token = new EmailActionToken
            {
                TokenValue = Guid.NewGuid().ToString("N"), // More secure token if needed
                RequestId = requestId,
                UserEmail = userEmail,
                ActionType = actionType,
                OptionId = optionId,
                ExpiresAt = DateTime.UtcNow.Add(expiryDuration.Value),
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.EmailActionTokens.Add(token);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Generated token {TokenValue} for RequestId {RequestId}, Action {ActionType}, User {UserEmail}", token.TokenValue, requestId, actionType, userEmail);
            return token.TokenValue;
        }

        public async Task<EmailActionToken?> ValidateAndConsumeTokenAsync(string tokenValue)
        {
            var token = await _context.EmailActionTokens
                .FirstOrDefaultAsync(t => t.TokenValue == tokenValue);

            if (token == null)
            {
                _logger.LogWarning("Token validation failed: Token {TokenValue} not found.", tokenValue);
                return null;
            }

            if (token.IsUsed)
            {
                _logger.LogWarning("Token validation failed: Token {TokenValue} already used.", tokenValue);
                return null;
            }

            if (token.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Token validation failed: Token {TokenValue} expired at {ExpiryTime}.", tokenValue, token.ExpiresAt);
                return null;
            }

            token.IsUsed = true;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Token {TokenValue} validated and consumed successfully.", tokenValue);
            return token;
        }

        public async Task<EmailActionToken?> PeekTokenAsync(string tokenValue)
        {
            return await _context.EmailActionTokens
               .AsNoTracking() // No need to track changes for a peek
               .FirstOrDefaultAsync(t => t.TokenValue == tokenValue);
        }
    }
}
