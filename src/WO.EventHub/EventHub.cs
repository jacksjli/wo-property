using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace WO.Property.EventHub;

public interface IEventHub
{
    Task PublishAsync(string eventType, object data);
}

public class EventHub : IEventHub
{
    private readonly ILogger<EventHub> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _gatewayUrl;

    public EventHub(
        ILogger<EventHub> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _gatewayUrl = configuration["GatewayUrl"] ?? "http://localhost:5000";
    }

    public async Task PublishAsync(string eventType, object data)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var payload = JsonSerializer.Serialize(new
            {
                type = eventType,
                data = data
            });

            using var ws = new ClientWebSocket();
            await ws.ConnectAsync(new Uri($"{_gatewayUrl.Replace("http", "ws")}/ws"), CancellationToken.None);

            if (ws.State == WebSocketState.Open)
            {
                var bytes = Encoding.UTF8.GetBytes(payload);
                await ws.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                );

                _logger.LogInformation("Event published: {EventType}", eventType);
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event: {EventType}", eventType);
        }
    }
}
