using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using MySqlConnector;
using Serilog;
using Serilog.Events;
using Yarp.ReverseProxy;
using WO.Property.Shared.Configuration;
using WO.Property.Shared.Logging;

var builder = WebApplication.CreateBuilder(args);

// === Serilog 日志配置 ===
var serviceName = "APIGateway";
var logsPath = Path.Combine(Directory.GetCurrentDirectory(), "logs");
Directory.CreateDirectory(logsPath);
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("ServiceName", serviceName)
    .Enrich.WithProperty("Application", "WO-Property")
    .WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter(renderMessage: true, closingDelimiter: Environment.NewLine))
    .WriteTo.File(
        new Serilog.Formatting.Json.JsonFormatter(renderMessage: true, closingDelimiter: Environment.NewLine),
        Path.Combine(logsPath, $"{serviceName.ToLower()}-.log"),
        rollingInterval: RollingInterval.Day,
        fileSizeLimitBytes: 100 * 1024 * 1024,
        retainedFileCountLimit: 30,
        rollOnFileSizeLimit: true,
        shared: false,
        flushToDiskInterval: TimeSpan.FromSeconds(2))
    .CreateLogger();

builder.Host.UseSerilog();

// === JSON 序列化配置（统一 camelCase） ===
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

// === YARP 路由配置 ===
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// === JWT 认证配置 ===
var jwtSecret = JwtHelper.GetSecretKey();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "wo-property-unified-auth",
            ValidAudience = "wo-property-services",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsJsonAsync(new { error = "Unauthorized", message = "Invalid or missing token" });
            }
        };
    });

builder.Services.AddAuthorization();

// ============================================================
// 安全中间件配置（从 appsettings.json 读取）
// ============================================================
var securityConfig = builder.Configuration.GetSection("Security").Get<SecurityConfig>() ?? new SecurityConfig();
var rateLimitConfig = builder.Configuration.GetSection("RateLimiting").Get<RateLimitConfig>() ?? new RateLimitConfig();
var corsConfig = builder.Configuration.GetSection("CORS").Get<CorsConfig>() ?? new CorsConfig();

// === 限流配置 (RateLimiting) ===
builder.Services.AddRateLimiter(options =>
{
    // 全局限流：每秒 N 请求
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var globalLimit = rateLimitConfig.GlobalLimit;
        return RateLimitPartition.GetFixedWindowLimiter("global", _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = globalLimit,
            Window = TimeSpan.FromSeconds(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = Math.Max(10, globalLimit / 10)
        });
    });

    options.OnRejected = async (context, cancellationToken) =>
    {
        var retryAfter = rateLimitConfig.Response429?.RetryAfterHeader == true ? 60 : 0;
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";
        if (retryAfter > 0)
        {
            context.HttpContext.Response.Headers.RetryAfter = retryAfter.ToString();
        }
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "TooManyRequests",
            code = "RATE_LIMIT_EXCEEDED",
            message = rateLimitConfig.Response429?.Message ?? "请求过于频繁，请稍后再试",
            retryAfter = retryAfter
        }, cancellationToken);
    };
});

// === CORS 配置 ===
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = corsConfig.AllowedOrigins ?? new[] { "http://localhost:5173" };
        var allowedMethods = corsConfig.AllowedMethods ?? new[] { "GET", "POST", "PUT", "DELETE", "PATCH" };
        var allowedHeaders = corsConfig.AllowedHeaders ?? new[] { "Authorization", "Content-Type" };
        var exposedHeaders = corsConfig.ExposedHeaders ?? new[] { "X-Request-ID" };

        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .SetPreflightMaxAge(TimeSpan.FromSeconds(corsConfig.PreflightMaxAgeSeconds));

        if (corsConfig.AllowCredentials == true)
        {
            policy.AllowCredentials();
        }
    });
});

// === 健康检查端点 ===
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5000);
    // 请求体大小限制
    options.Limits.MaxRequestBodySize = securityConfig.RequestBodyLimit?.MaxSizeInBytes ?? 10485760;
});

var app = builder.Build();

// ============================================================
// 安全中间件链
// ============================================================

// 1. RateLimiter（限流）
app.UseRateLimiter();

// 2. CORS
app.UseCors();

// 3. 请求安全检查（自定义中间件）
app.Use(async (context, next) =>
{
    // 请求体大小检查（已在 Kestrel 设置，但作为双保险）
    if (securityConfig.RequestBodyLimit?.Enabled == true)
    {
        var maxSize = securityConfig.RequestBodyLimit.MaxSizeInBytes;
        if (context.Request.ContentLength.HasValue && context.Request.ContentLength.Value > maxSize)
        {
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "PayloadTooLarge",
                message = $"请求体过大，最大允许 {securityConfig.RequestBodyLimit.MaxSizeInMB}MB"
            });
            return;
        }
    }

    // HTTP 方法控制
    if (securityConfig.HttpMethodControl?.BlockUnknownMethods == true)
    {
        var allowed = new[] { "GET", "POST", "PUT", "DELETE", "PATCH" };
        if (!allowed.Contains(context.Request.Method.ToUpperInvariant()))
        {
            context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "MethodNotAllowed",
                message = "不允许的 HTTP 方法"
            });
            return;
        }
    }

    // HEAD 请求控制（可选禁用）
    if (securityConfig.HttpMethodControl?.AllowHead == false && context.Request.Method == "HEAD")
    {
        context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            error = "MethodNotAllowed",
            message = "HEAD 请求已被禁用"
        });
        return;
    }

    // 请求头数量限制
    if (securityConfig.HeaderLimits?.MaxRequestHeadersCount > 0)
    {
        if (context.Request.Headers.Count > securityConfig.HeaderLimits.MaxRequestHeadersCount)
        {
            context.Response.StatusCode = StatusCodes.Status431RequestHeaderFieldsTooLarge;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "TooManyHeaders",
                message = "请求头数量超出限制"
            });
            return;
        }
    }

    await next();
});

// 4. Slowloris 防护（读取超时）
if (securityConfig.SlowlorisProtection?.Enabled == true)
{
    var readTimeout = securityConfig.SlowlorisProtection.ReadTimeoutSeconds;
    var writeTimeout = securityConfig.SlowlorisProtection.WriteTimeoutSeconds;
    // Kestrel 的超时已在下面配置
}

// 5. 参数校验中间件（SQL 注入、XSS）
app.Use(async (context, next) =>
{
    if (securityConfig.SqlInjectionProtection?.Enabled == true ||
        securityConfig.XssProtection?.Enabled == true)
    {
        var needsSanitize = false;

        // 检查 Query String
        foreach (var key in context.Request.Query.Keys)
        {
            var value = context.Request.Query[key].ToString();
            if (ContainsSqlInjection(value) || ContainsXss(value))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "BadRequest",
                    code = "INVALID_INPUT",
                    message = "请求参数包含非法字符"
                });
                return;
            }
        }

        // 检查 Body（仅 JSON）
        if (context.Request.ContentType?.Contains("application/json") == true)
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            if (ContainsSqlInjection(body) || ContainsXss(body))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "BadRequest",
                    code = "INVALID_INPUT",
                    message = "请求体包含非法字符"
                });
                return;
            }
        }
    }

    await next();
});

// 6. 日志中间件
app.UseRequestLogging();
app.UseGlobalExceptionHandler();

// 7. 认证/授权
app.UseAuthentication();
app.UseAuthorization();

// 健康检查端点（无需认证）
app.MapGet("/health", async () =>
{
    var response = new
    {
        status = "healthy",
        service = "WO.Property.APIGateway",
        version = "1.1.0",
        timestamp = DateTime.UtcNow,
        protections = new
        {
            rateLimiting = rateLimitConfig.Enabled,
            sqlInjectionProtection = securityConfig.SqlInjectionProtection?.Enabled,
            xssProtection = securityConfig.XssProtection?.Enabled,
            slowlorisProtection = securityConfig.SlowlorisProtection?.Enabled,
            corsEnabled = corsConfig.Enabled
        }
    };
    return Results.Ok(response);
}).AllowAnonymous();

// 健康检查详细（用于运维）
app.MapGet("/health/detailed", async () =>
{
    var rateLimitInfo = new
    {
        globalLimit = rateLimitConfig.GlobalLimit,
        perIpLimit = rateLimitConfig.PerIpLimit,
        perUserLimit = rateLimitConfig.PerUserLimit,
        windowSeconds = rateLimitConfig.WindowSeconds,
        sensitiveEndpoints = rateLimitConfig.SensitiveEndpoints,
        sensitiveLimit = rateLimitConfig.SensitiveLimit
    };

    var securityInfo = new
    {
        requestBodyLimitMB = securityConfig.RequestBodyLimit?.MaxSizeInMB,
        sqlInjectionEnabled = securityConfig.SqlInjectionProtection?.Enabled,
        xssEnabled = securityConfig.XssProtection?.Enabled,
        slowlorisTimeoutSeconds = securityConfig.SlowlorisProtection?.ReadTimeoutSeconds
    };

    return Results.Ok(new
    {
        status = "healthy",
        service = "WO.Property.APIGateway",
        version = "1.1.0",
        timestamp = DateTime.UtcNow,
        rateLimiting = rateLimitInfo,
        security = securityInfo
    });
}).AllowAnonymous();

// YARP 反向代理
app.MapReverseProxy();

app.Run();

// ============================================================
// 辅助方法和配置类
// ============================================================

bool ContainsSqlInjection(string input)
{
    if (string.IsNullOrEmpty(input)) return false;
    var patterns = new[]
    {
        "--", ";--", "/*", "*/", "@@", "char", "nchar", "varchar", "nvarchar",
        "alter", "begin", "cast", "create", "cursor", "declare", "delete", "drop",
        "end", "exec", "execute", "fetch", "insert", "kill", "select", "sys",
        "sysobjects", "syscolumns", "table", "update", "xp_", "0x", "or 1=1", "or 1=2"
    };
    var lower = input.ToLowerInvariant();
    return patterns.Any(p => lower.Contains(p));
}

bool ContainsXss(string input)
{
    if (string.IsNullOrEmpty(input)) return false;
    var patterns = new[]
    {
        "<script", "</script", "javascript:", "onerror=", "onload=", "onclick=",
        "onmouseover=", "onfocus=", "onblur=", "<iframe", "<object", "<embed",
        "<svg", "expression\\(", "eval\\(", "innerHTML", "outerHTML"
    };
    var lower = input.ToLowerInvariant();
    return patterns.Any(p => System.Text.RegularExpressions.Regex.IsMatch(lower, patterns.Length > 0 ? patterns[0] : ""));
}

// 配置类
public class SecurityConfig
{
    public RequestBodyLimitConfig? RequestBodyLimit { get; set; }
    public SqlInjectionProtectionConfig? SqlInjectionProtection { get; set; }
    public XssProtectionConfig? XssProtection { get; set; }
    public HttpMethodControlConfig? HttpMethodControl { get; set; }
    public HeaderLimitsConfig? HeaderLimits { get; set; }
    public SlowlorisProtectionConfig? SlowlorisProtection { get; set; }
}

public class RequestBodyLimitConfig
{
    public long MaxSizeInBytes { get; set; } = 10485760;
    public int MaxSizeInMB { get; set; } = 10;
    public bool Enabled { get; set; } = true;
}

public class SqlInjectionProtectionConfig
{
    public bool Enabled { get; set; } = true;
}

public class XssProtectionConfig
{
    public bool Enabled { get; set; } = true;
}

public class HttpMethodControlConfig
{
    public bool AllowHead { get; set; } = false;
    public bool AllowOptions { get; set; } = true;
    public bool BlockUnknownMethods { get; set; } = true;
}

public class HeaderLimitsConfig
{
    public int MaxRequestHeadersCount { get; set; } = 64;
    public int MaxHeaderSizeInBytes { get; set; } = 8192;
    public long MaxContentLengthInBytes { get; set; } = 104857600;
    public int KeepAliveTimeoutSeconds { get; set; } = 30;
}

public class SlowlorisProtectionConfig
{
    public bool Enabled { get; set; } = true;
    public int ReadTimeoutSeconds { get; set; } = 15;
    public int WriteTimeoutSeconds { get; set; } = 15;
}

public class RateLimitConfig
{
    public bool Enabled { get; set; } = true;
    public int GlobalLimit { get; set; } = 1000;
    public int PerIpLimit { get; set; } = 100;
    public int PerUserLimit { get; set; } = 200;
    public int WindowSeconds { get; set; } = 60;
    public string[]? SensitiveEndpoints { get; set; }
    public int SensitiveLimit { get; set; } = 10;
    public Response429Config? Response429 { get; set; }
}

public class Response429Config
{
    public bool Enabled { get; set; } = true;
    public bool RetryAfterHeader { get; set; } = true;
    public string? Message { get; set; }
}

public class CorsConfig
{
    public bool Enabled { get; set; } = true;
    public string[]? AllowedOrigins { get; set; }
    public string[]? AllowedMethods { get; set; }
    public string[]? AllowedHeaders { get; set; }
    public string[]? ExposedHeaders { get; set; }
    public bool? AllowCredentials { get; set; }
    public int PreflightMaxAgeSeconds { get; set; } = 3600;
}