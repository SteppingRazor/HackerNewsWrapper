using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace HackerNewsWrapper.Story.API.Services;

public class CacheService : ICacheService
{
    private readonly IDistributedCache _cache;

    public CacheService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var value = await _cache.GetStringAsync(key);

        if (value != null)
        {
            return JsonConvert.DeserializeObject<T>(value);
        }

        return default;
    }

    public async Task<T?> SetAsync<T>(string key, T value)
    {
        await _cache.SetStringAsync(key, JsonConvert.SerializeObject(value), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        });
        return value;
    }

    public async Task RemoveAsync(string key)
    {
        await _cache.RemoveAsync(key);
    }
}
