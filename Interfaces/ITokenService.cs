using ShareVault.API.Models;

namespace ShareVault.API.Interfaces
{
    public interface ITokenService
    {
        Task<string> GenerateTokenAsync(User user);
        RefreshToken GenerateRefreshToken(User user);
        Task<RefreshToken?> GetRefreshTokenAsync(string token);
        Task RevokeRefreshTokenAsync(string token, string reason = "Token revoked");
        Task RevokeAllUserRefreshTokensAsync(string userId, string reason = "User logged out");
    }
} 