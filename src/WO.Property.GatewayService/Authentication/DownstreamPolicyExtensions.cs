using Polly;
using Polly.Timeout;
using Polly.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace YARP.Gateway.Authentication;

/// <summary>
/// 下游服务熔断 + 重试策略配置
/// 使用 Polly 实现
/// </summary>
public static class DownstreamServicePolicies
{
    /// <summary>
    /// 指数退避重试策略
    /// - 最多重试 3 次
    /// - 间隔：2s → 4s → 8s（指数退避）
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> RetryPolicy { get; } = Policy
        .HandleResult<HttpResponseMessage>(r =>
            r.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
            r.StatusCode == System.Net.HttpStatusCode.GatewayTimeout ||
            r.StatusCode == System.Net.HttpStatusCode.RequestTimeout)
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
            onRetry: (outcome, timespan, retryAttempt, context) =>
            {
                context["Logger"]?.GetType()?.GetMethod("LogWarning")?.Invoke(
                    context["Logger"],
                    new object[] { "Downstream request failed, retry {RetryAttempt} in {Delay}ms. Status: {Status}", retryAttempt, timespan.TotalMilliseconds, outcome.Result!.StatusCode });
            });

    /// <summary>
    /// 熔断器策略
    /// - 50% 请求失败后熔断 30 秒
    /// - 熔断期间快速失败，不发请求到下游
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> CircuitBreakerPolicy { get; } = Policy
        .HandleResult<HttpResponseMessage>(r =>
            r.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
            r.StatusCode == System.Net.HttpStatusCode.GatewayTimeout)
        .AdvancedCircuitBreakerAsync(
            failureThreshold: 0.5,
            minimumThroughput: 10,
            samplingDuration: TimeSpan.FromSeconds(30),
            durationOfBreak: TimeSpan.FromSeconds(30),
            onBreak: (outcome, breakDelay) =>
            {
                Console.WriteLine($"[CircuitBreaker] Opened for {breakDelay.TotalSeconds}s due to downstream failure");
            },
            onReset: () =>
            {
                Console.WriteLine("[CircuitBreaker] Closed - downstream service recovered");
            },
            onHalfOpen: () =>
            {
                Console.WriteLine("[CircuitBreaker] Half-open - testing downstream service");
            });

    /// <summary>
    /// 超时策略（单次请求最多 30 秒）
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> TimeoutPolicy { get; } = Policy
        .TimeoutAsync<HttpResponseMessage>(
            TimeSpan.FromSeconds(30),
            TimeoutStrategy.Optimistic);
}

public static class DownstreamPolicyExtensions
{
    public static IServiceCollection AddDownstreamServicePolicies(this IServiceCollection services)
    {
        // 将策略注册为单例（供 HttpClientFactory 使用）
        services.AddSingleton<Polly.IAsyncPolicy<System.Net.Http.HttpResponseMessage>>(
            DownstreamServicePolicies.RetryPolicy);
        services.AddSingleton<Polly.IAsyncPolicy<System.Net.Http.HttpResponseMessage>>(
            DownstreamServicePolicies.CircuitBreakerPolicy);

        return services;
    }
}
