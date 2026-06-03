using YARP.Gateway.Authentication;
using YARP.Gateway.Middleware;
using YARP.Gateway.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using WO.Property.GatewayService;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// 1. YARP 路由配置（从 appsettings.json 加载）
// ============================================================
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("YARP"));

// ============================================================
// 2. JWT 认证（统一入口，所有下游服务共享）
// ============================================================
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"]
    ?? throw new InvalidOperationException("JwtSettings:SecretKey is required");
var issuer = jwtSettings["Issuer"] ?? "wo-property-unified-auth";
var audience = jwtSettings["Audience"] ?? "wo-property-services";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero
        };
        // 允许 token 从 query string 传递（移动端场景）
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (string.IsNullOrEmpty(context.Request.Headers.Authorization.FirstOrDefault()))
                {
                    var token = context.Request.Query["access_token"].FirstOrDefault();
                    if (!string.IsNullOrEmpty(token))
                        context.Token = token;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ============================================================
// 3. 下游服务熔断（Polly）+ 重试策略
// ============================================================
builder.Services.AddDownstreamServicePolicies();

// ============================================================
// 4. 全局限流
// ============================================================
builder.Services.AddSingleton<IRateLimiter, TokenBucketRateLimiter>();

// ============================================================
// 5. Swagger（网关统一入口文档）
// ============================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "WO Property API Gateway",
        Version = "v1",
        Description = "物业管理软件统一 API 入口 - 服务治理基础"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ============================================================
// 6. CORS
// ============================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins")
            .Get<string[]>() ?? new[] { "http://localhost:5173" };
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// WebSocket 管理器
builder.Services.AddWebSocketManager();

var app = builder.Build();

// ============================================================
// 6. 中间件管道
// ============================================================
// CORS 必须放在 WebSocket 之前，因为握手需要 CORS 头
app.UseCors("AllowFrontend");

// WebSocket 支持（必须在 UseWebSocketManager 之前）
// 注意：WebSocket 握手必须在认证之前，否则会被 auth 中间件拦截导致 403
app.UseWebSockets();
app.UseWebSocketManager();

// Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 自定义中间件：请求日志 + 项目隔离 Header（放在 auth 之前，避免对 /ws 的干扰）
app.UseRequestLogging();
app.UseProjectIsolation();

// 认证（用于网关本身验证，如 /me）
app.UseAuthentication();
app.UseAuthorization();

// 健康检查
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "WO.Property.GatewayService",
    version = "1.0.0",
    timestamp = DateTime.UtcNow,
    features = new[]
    {
        "YARP 智能路由",
        "JWT 统一认证",
        "下游服务熔断",
        "全局限流",
        "项目隔离 Header",
        "统一 Swagger 文档"
    }
})).AllowAnonymous();

// 路由转发
app.MapReverseProxy();

// 事件 API - 统一事件发布端点，所有服务通过此端点发布事件
var wsManager = app.Services.GetRequiredService<WO.Property.GatewayService.WebSocketManager>();

// 通用事件发布端点 (旧版，保留兼容)
app.MapPost("/internal/events/ticket", async (TicketEventPayload payload) =>
{
    Console.WriteLine($"[Events] Received: {payload.EventType} - TicketId: {payload.TicketId}");
    await wsManager.BroadcastAsync("ticketUpdated", payload);
    return Results.Ok(new { success = true, connections = wsManager.GetConnectionCount() });
});

// 统一事件发布端点 (新版，所有模块通用)
app.MapPost("/internal/events/publish", async (PublishEventRequest request) =>
{
    Console.WriteLine($"[Events] {request.Module}:{request.EventType}");
    var eventType = $"{request.Module}:{request.EventType}";
    await wsManager.BroadcastAsync(eventType, request);
    return Results.Ok(new { success = true, connections = wsManager.GetConnectionCount() });
});

app.Run();

public class TicketEventPayload
{
    public string EventType { get; set; } = "";
    public int TicketId { get; set; }
    public string? TicketCode { get; set; }
    public string? Title { get; set; }
    public string? Status { get; set; }
    public string? PreviousStatus { get; set; }
    public object? ExtraData { get; set; }
}

public class PublishEventRequest
{
    public string Module { get; set; } = "";
    public string EventType { get; set; } = "";
    public object? Data { get; set; }
}
