using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace WO.Property.GatewayService;

public class WebSocketManager
{
    private readonly ConcurrentDictionary<string, WebSocket> _sockets = new();
    private readonly ILogger<WebSocketManager> _logger;

    public WebSocketManager(ILogger<WebSocketManager> logger)
    {
        _logger = logger;
    }

    public string AddSocket(WebSocket socket)
    {
        var id = Guid.NewGuid().ToString();
        _sockets.TryAdd(id, socket);
        _logger.LogInformation("WebSocket connected: {Id}", id);
        return id;
    }

    public async Task RemoveSocket(string id)
    {
        if (_sockets.TryRemove(id, out var socket))
        {
            try
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                socket.Dispose();
            }
            catch { }
            _logger.LogInformation("WebSocket disconnected: {Id}", id);
        }
    }

    public async Task BroadcastAsync(string eventType, object data)
    {
        var message = JsonSerializer.Serialize(new { type = eventType, data });
        var bytes = Encoding.UTF8.GetBytes(message);

        foreach (var socket in _sockets.Values.ToList())
        {
            if (socket.State == WebSocketState.Open)
            {
                try
                {
                    await socket.SendAsync(
                        new ArraySegment<byte>(bytes),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send WebSocket message");
                }
            }
        }
    }

    public int GetConnectionCount() => _sockets.Count;
}

public class WebSocketHandler
{
    private readonly RequestDelegate _next;
    private readonly WebSocketManager _manager;
    private readonly ILogger<WebSocketHandler> _logger;

    public WebSocketHandler(
        RequestDelegate next,
        WebSocketManager manager,
        ILogger<WebSocketHandler> logger)
    {
        _next = next;
        _manager = manager;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path == "/ws")
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var socket = await context.WebSockets.AcceptWebSocketAsync();
                var clientId = _manager.AddSocket(socket);

                // Send connection confirmation
                var welcomeMessage = JsonSerializer.Serialize(new { type = "connected", data = new { clientId, connections = _manager.GetConnectionCount() } });
                var welcomeBytes = Encoding.UTF8.GetBytes(welcomeMessage);
                await socket.SendAsync(
                    new ArraySegment<byte>(welcomeBytes),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                );

                // Keep connection alive and handle messages
                var buffer = new byte[1024 * 4];
                while (socket.State == WebSocketState.Open)
                {
                    try
                    {
                        var result = await socket.ReceiveAsync(
                            new ArraySegment<byte>(buffer),
                            CancellationToken.None
                        );

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await _manager.RemoveSocket(clientId);
                            break;
                        }
                    }
                    catch (WebSocketException)
                    {
                        await _manager.RemoveSocket(clientId);
                        break;
                    }
                }
            }
            else
            {
                context.Response.StatusCode = 400;
            }
        }
        else
        {
            await _next(context);
        }
    }
}



public class TicketEventPayload
{
    public string EventType { get; set; } = ""; // created, updated, status_changed, assigned
    public int TicketId { get; set; }
    public string? TicketCode { get; set; }
    public string? Title { get; set; }
    public string? Status { get; set; }
    public string? PreviousStatus { get; set; }
    public object? ExtraData { get; set; }
}

// Extension methods
public static class WebSocketExtensions
{
    public static IApplicationBuilder UseWebSocketManager(this IApplicationBuilder app)
    {
        app.UseMiddleware<WebSocketHandler>();
        return app;
    }

    public static IServiceCollection AddWebSocketManager(this IServiceCollection services)
    {
        services.AddSingleton<WebSocketManager>();
        return services;
    }
}
