using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace WO.Property.Shared.Events;

/// <summary>
/// 统一事件发布器 - 所有服务使用此工具发布事件到 Gateway
/// </summary>
public class EventPublisher
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<EventPublisher> _logger;
    private readonly string _gatewayUrl;

    public EventPublisher(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<EventPublisher> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _gatewayUrl = configuration["GatewayUrl"] ?? "http://localhost:5000";
    }

    /// <summary>
    /// 发布事件到 Gateway
    /// </summary>
    /// <param name="module">模块名: ticket, payment, notification, complaint, etc.</param>
    /// <param name="eventType">事件类型: created, updated, deleted, status_changed</param>
    /// <param name="data">事件数据</param>
    public async Task PublishAsync(string module, string eventType, object data)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("Gateway");
            var payload = new
            {
                module = module,
                eventType = eventType,
                type = $"{module}:{eventType}",
                data = data
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.PostAsync("/internal/events/publish", content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[Event] Published: {Module}:{EventType}", module, eventType);
            }
            else
            {
                _logger.LogWarning("[Event] Failed to publish: {Module}:{EventType}, Status: {Status}",
                    module, eventType, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Event] Publish exception: {Module}:{EventType}", module, eventType);
            // 不抛出异常，避免影响主业务流程
        }
    }

    /// <summary>
    /// 发布带额外数据的通用事件
    /// </summary>
    public async Task PublishAsync(string module, string eventType, object data, object extraData)
    {
        var payload = new
        {
            module = module,
            eventType = eventType,
            type = $"{module}:{eventType}",
            data = data,
            extraData = extraData
        };

        await PublishAsync(module, eventType, payload);
    }
}

/// <summary>
/// 通用事件数据对象
/// </summary>
public class EventData
{
    public int Id { get; set; }
    public string? Code { get; set; }
    public string? Status { get; set; }
    public string? Title { get; set; }
    public object? Extra { get; set; }
}