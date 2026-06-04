using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace BonusIdrici2.Services
{
    public class AppCacheService
    {
        public static readonly TimeSpan EntiExpiration = TimeSpan.FromMinutes(10);
        public static readonly TimeSpan ToponimiExpiration = TimeSpan.FromMinutes(15);
        public static readonly TimeSpan AnagrafeExpiration = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan UtenzeExpiration = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan ReportExpiration = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan StatisticheExpiration = TimeSpan.FromMinutes(3);

        private readonly IMemoryCache _cache;
        private readonly ILogger<AppCacheService> _logger;
        private readonly ConcurrentDictionary<string, byte> _keys = new();
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

        public AppCacheService(IMemoryCache cache, ILogger<AppCacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            if (_cache.TryGetValue(key, out T? cached))
            {
                _logger.LogDebug("Cache HIT: {CacheKey}", key);
                return cached!;
            }

            var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync();
            try
            {
                if (_cache.TryGetValue(key, out cached))
                {
                    _logger.LogDebug("Cache HIT: {CacheKey}", key);
                    return cached!;
                }

                _logger.LogDebug("Cache MISS: {CacheKey}", key);
                var value = await factory();
                Set(key, value, expiration);
                return value;
            }
            finally
            {
                semaphore.Release();
            }
        }

        public T GetOrCreate<T>(string key, Func<T> factory, TimeSpan? expiration = null)
        {
            if (_cache.TryGetValue(key, out T? cached))
            {
                _logger.LogDebug("Cache HIT: {CacheKey}", key);
                return cached!;
            }

            _logger.LogDebug("Cache MISS: {CacheKey}", key);
            var value = factory();
            Set(key, value, expiration);
            return value;
        }

        public void Remove(string key)
        {
            _cache.Remove(key);
            _keys.TryRemove(key, out _);
            _logger.LogDebug("Cache REMOVE: {CacheKey}", key);
        }

        public void RemoveByPrefix(string prefix)
        {
            foreach (var key in _keys.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList())
            {
                Remove(key);
            }

            _logger.LogDebug("Cache REMOVE PREFIX: {CachePrefix}", prefix);
        }

        public void ClearEnteCache(int idEnte)
        {
            Remove($"enti:detail:{idEnte}");
            RemoveByPrefix($"anagrafe:ente:{idEnte}");
            RemoveByPrefix($"utenze:ente:{idEnte}");
            RemoveByPrefix($"toponimi:ente:{idEnte}");
            RemoveByPrefix($"reports:ente:{idEnte}");
            RemoveByPrefix($"statistiche:ente:{idEnte}");
            Remove("dashboard:admin");
        }

        public void ClearUserCache(int idUser)
        {
            Remove($"enti:user:{idUser}");
            Remove($"dashboard:user:{idUser}");
        }

        public void ClearReportCache(int idReport)
        {
            Remove($"report:detail:{idReport}");
            Remove($"domande:report:{idReport}");
        }

        public void ClearAll()
        {
            foreach (var key in _keys.Keys.ToList())
            {
                Remove(key);
            }

            _logger.LogInformation("Cache applicativa svuotata manualmente.");
        }

        private void Set<T>(string key, T value, TimeSpan? expiration)
        {
            _cache.Set(key, value, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(5)
            });
            _keys.TryAdd(key, 0);
        }
    }
}
