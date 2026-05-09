using YARP.Gateway.Authentication;
using YARP.Gateway.Middleware;
using YARP.Gateway.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;

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

var app = builder.Build();

// ============================================================
// 7. 中间件管道
// ============================================================
app.UseCors("AllowFrontend");

// Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 认证（用于网关本身验证，如 /me）
app.UseAuthentication();
app.UseAuthorization();

// 自定义中间件：请求日志 + 项目隔离 Header
app.UseRequestLogging();
app.UseProjectIsolation();

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

app.Run();
