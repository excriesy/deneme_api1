using Microsoft.Extensions.Caching.Memory;

namespace ShareVault.API.Services
{
    public interface IBruteForceProtectionService
    {
        bool IsBlocked(string ip);
        void RegisterFailedAttempt(string ip);
        void ResetAttempts(string ip);
    }

    public class BruteForceProtectionService : IBruteForceProtectionService
    {
        private readonly IMemoryCache _cache;
        private const int MaxAttempts = 5;
        private static readonly TimeSpan BlockDuration = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan AttemptWindow = TimeSpan.FromMinutes(10);

        public BruteForceProtectionService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public bool IsBlocked(string ip)
        {
            if (_cache.TryGetValue($"block_{ip}", out _))
                return true;
            return false;
        }

        public void RegisterFailedAttempt(string ip)
        {
            var key = $"fail_{ip}";
            int attempts = _cache.Get<int?>(key) ?? 0;
            attempts++;
            if (attempts >= MaxAttempts)
            {
                _cache.Set($"block_{ip}", true, BlockDuration);
                _cache.Remove(key);
            }
            else
            {
                _cache.Set(key, attempts, AttemptWindow);
            }
        }

        public void ResetAttempts(string ip)
        {
            _cache.Remove($"fail_{ip}");
            _cache.Remove($"block_{ip}");
        }
    }
} 