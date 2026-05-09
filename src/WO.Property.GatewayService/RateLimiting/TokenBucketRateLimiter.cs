using System.Collections.Concurrent;

namespace YARP.Gateway.RateLimiting;

/// <summary>
/// 基于令牌桶算法的限流器
/// - high: 100 req/min（认证相关接口）
/// - medium: 300 req/min（一般业务接口）
/// - low: 600 req/min（查询/只读接口）
/// </summary>
public interface IRateLimiter
{
    bool AllowRequest(string clientId, string tier);
}

public class TokenBucketRateLimiter : IRateLimiter
{
    private readonly ConcurrentDictionary<string, TokenBucket> _buckets = new();
    private readonly ILogger<TokenBucketRateLimiter> _logger;

    private static readonly Dictionary<string, (int capacity, int refillPerMinute)> TierLimits = new()
    {
        ["high"]   = (capacity: 20,  refillPerMinute: 20),   // 认证接口：20次/分钟
        ["medium"] = (capacity: 60,  refillPerMinute: 60),   // 一般接口：60次/分钟
        ["low"]    = (capacity: 120, refillPerMinute: 120),  // 只读接口：120次/分钟
    };

    public TokenBucketRateLimiter(ILogger<TokenBucketRateLimiter> logger)
    {
        _logger = logger;
        // 启动后台清理线程（移除长时间不活跃的 bucket）
        _ = Task.Run(CleanupLoop);
    }

    public bool AllowRequest(string clientId, string tier)
    {
        var limits = TierLimits.TryGetValue(tier, out var l) ? l : TierLimits["low"];
        var bucket = _buckets.GetOrAdd($"{clientId}:{tier}", _ => new TokenBucket(limits.capacity, limits.refillPerMinute));
        return bucket.TryConsume();
    }

    private async Task CleanupLoop()
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromMinutes(10));

            var threshold = DateTime.UtcNow.AddMinutes(-30);
            var keysToRemove = _buckets
                .Where(kv => kv.Value.LastRefill < threshold)
                .Select(kv => kv.Key)
                .ToList();

            foreach (var key in keysToRemove)
                _buckets.TryRemove(key, out _);

            if (keysToRemove.Count > 0)
                _logger.LogDebug("Cleaned up {Count} inactive rate limit buckets", keysToRemove.Count);
        }
    }
}

internal class TokenBucket
{
    private readonly int _capacity;
    private readonly double _refillPerSecond;
    private double _tokens;
    private readonly object _lock = new();
    private DateTime _lastRefill;

    public DateTime LastRefill => _lastRefill;

    public TokenBucket(int capacity, int refillPerMinute)
    {
        _capacity = capacity;
        _refillPerSecond = refillPerMinute / 60.0;
        _tokens = capacity;
        _lastRefill = DateTime.UtcNow;
    }

    public bool TryConsume(int count = 1)
    {
        lock (_lock)
        {
            Refill();
            if (_tokens >= count)
            {
                _tokens -= count;
                return true;
            }
            return false;
        }
    }

    private void Refill()
    {
        var now = DateTime.UtcNow;
        var elapsed = (now - _lastRefill).TotalSeconds;
        var refill = elapsed * _refillPerSecond;
        _tokens = Math.Min(_capacity, _tokens + refill);
        _lastRefill = now;
    }
}
