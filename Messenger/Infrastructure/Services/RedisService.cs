using StackExchange.Redis;
using System.Text.Json;

namespace Messenger.Infrastructure.Services;

public interface IRedisService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task DeleteAsync(string key);
    Task PublishAsync<T>(string channel, T message);
    Task SubscribeAsync<T>(string channel, Action<T> handler);
    Task<bool> KeyExistsAsync(string key);
    Task SetExpiryAsync(string key, TimeSpan expiry);
}

public class RedisService(IConnectionMultiplexer redis) : IRedisService
{
    private readonly IConnectionMultiplexer _redis = redis;
    private readonly IDatabase _database = redis.GetDatabase();
    private readonly ISubscriber _subscriber = redis.GetSubscriber();

    public async Task<T?> GetAsync<T>(string key)
    {
        var value = await _database.StringGetAsync(key);
        return value.HasValue ? JsonSerializer.Deserialize<T>(value!) : default;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var serializedValue = JsonSerializer.Serialize(value);
        await _database.StringSetAsync(key, serializedValue, expiry);
    }

    public async Task DeleteAsync(string key)
    {
        await _database.KeyDeleteAsync(key);
    }

    public async Task PublishAsync<T>(string channel, T message)
    {
        var serializedMessage = JsonSerializer.Serialize(message);
        await _subscriber.PublishAsync(channel, serializedMessage);
    }

    public async Task SubscribeAsync<T>(string channel, Action<T> handler)
    {
        await _subscriber.SubscribeAsync(channel, (_, value) =>
        {
            var message = JsonSerializer.Deserialize<T>(value!);
            if (message != null)
            {
                handler(message);
            }
        });
    }

    public async Task<bool> KeyExistsAsync(string key)
    {
        return await _database.KeyExistsAsync(key);
    }

    public async Task SetExpiryAsync(string key, TimeSpan expiry)
    {
        await _database.KeyExpireAsync(key, expiry);
    }
} 