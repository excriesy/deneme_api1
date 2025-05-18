using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace ShareVault.API.Services
{
    public interface ICacheService
    {
        T Get<T>(string key);
        void Set<T>(string key, T value, TimeSpan? expiration = null);
        void Remove(string key);
        bool Exists(string key);
    }

    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ConcurrentDictionary<string, DateTime> _expirationTimes;

        public CacheService(IMemoryCache cache)
        {
            _cache = cache;
            _expirationTimes = new ConcurrentDictionary<string, DateTime>();
        }

        public T Get<T>(string key)
        {
            if (_cache.TryGetValue(key, out T value))
            {
                if (_expirationTimes.TryGetValue(key, out DateTime expirationTime))
                {
                    if (DateTime.UtcNow > expirationTime)
                    {
                        Remove(key);
                        return default;
                    }
                }
                return value;
            }
            return default;
        }

        public void Set<T>(string key, T value, TimeSpan? expiration = null)
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions();
            
            if (expiration.HasValue)
            {
                cacheEntryOptions.AbsoluteExpirationRelativeToNow = expiration;
                _expirationTimes[key] = DateTime.UtcNow.Add(expiration.Value);
            }

            _cache.Set(key, value, cacheEntryOptions);
        }

        public void Remove(string key)
        {
            _cache.Remove(key);
            _expirationTimes.TryRemove(key, out _);
        }

        public bool Exists(string key)
        {
            return _cache.TryGetValue(key, out _);
        }
    }
} 