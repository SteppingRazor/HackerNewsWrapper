﻿namespace HackerNewsWrapper.Story.API.Services;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task<T?> SetAsync<T>(string key, T value);
    Task RemoveAsync(string key);
}
