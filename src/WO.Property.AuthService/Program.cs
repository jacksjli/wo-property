using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MySqlConnector;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WO.Property.Shared.Configuration;
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 配置端口 - Phase 0 临时使用 5106
builder.WebHost.UseUrls("http://0.0.0.0:5106");

// 连接字符串
var centerDbConnectionString = Environment.GetEnvironmentVariable("CENTER_DB_CONNECTION_STRING")
    ?? "Server=127.0.0.1;Port=3306;Database=center_db;User=root;Password=;CharSet=utf8mb4;";

var tenantDbConnectionStringTemplate = Environment.GetEnvironmentVariable("TENANT_DB_CONNECTION_TEMPLATE")
    ?? "Server=127.0.0.1;Port=3306;Database={db_name};User=root;Password=;CharSet=utf8mb4;";

var jwtSecretKey = JwtHelper.GetSecretKey();

// JWT 密钥（从环境变量 JWT_SECRET_KEY 读取）
builder.Services.AddScoped<MySqlConnection>(_ => new MySqlConnection(centerDbConnectionString));

// 添加服务
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddEndpointsApiExplorer();

// 配置JWT认证
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey))
        };
    });

builder.Services.AddAuthorization();

// 配置CORS
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins")
    .Get<string[]>() ?? new[] { "http://localhost:5173" };
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// === 登录防护配置 ===
var loginProtectionConfig = builder.Configuration.GetSection("LoginProtection").Get<LoginProtectionConfig>()
    ?? new LoginProtectionConfig();

// === 登录防护内存存储 ===
var loginFailures = new ConcurrentDictionary<string, LoginFailureRecord>();
var ipFailures = new ConcurrentDictionary<string, LoginFailureRecord>();

var app = builder.Build();

// 中间件
app.UseCors("AllowFrontend");

// 登录防护中间件
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
    var method = context.Request.Method;

    if (path.Contains("/api/auth/login") && method == "POST")
    {
        var ip = GetClientIp(context);
        var now = DateTime.UtcNow;

        if (ipFailures.TryGetValue(ip, out var ipRecord))
        {
            if (now < ipRecord.LockoutEnd)
            {
                var remainingSeconds = (int)(ipRecord.LockoutEnd.Value - now).TotalSeconds;
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.ContentType = "application/json";
                context.Response.Headers.RetryAfter = remainingSeconds.ToString();
                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    error = "IpBanned",
                    code = "IP_TEMPORARILY_BANNED",
                    message = $"IP 已被临时封禁，请在 {remainingSeconds} 秒后重试",
                    bannedUntil = ipRecord.LockoutEnd.Value.ToString("O")
                });
                return;
            }
            else
            {
                ipFailures.TryRemove(ip, out _);
            }
        }

        var config = builder.Configuration.GetSection("LoginProtection").Get<LoginProtectionConfig>()
            ?? new LoginProtectionConfig();

        var recentFailures = ipFailures.Values
            .Where(r => r.Ip == ip && (now - r.AttemptTime).TotalSeconds < config.BanDurationSeconds)
            .Count();

        if (recentFailures >= config.MaxFailuresBeforeBan)
        {
            var lockoutEnd = now.AddSeconds(config.BanDurationSeconds);
            ipFailures[ip] = new LoginFailureRecord
            {
                Ip = ip,
                AttemptTime = now,
                LockoutEnd = lockoutEnd,
                FailureCount = recentFailures + 1
            };

            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                error = "TooManyAttempts",
                code = "IP_BANNED",
                message = $"检测到异常登录行为，IP 已临时封禁 {config.BanDurationSeconds / 60} 分钟",
                bannedUntil = lockoutEnd.ToString("O")
            });
            return;
        }
    }

    await next();
});

app.UseAuthentication();
app.UseAuthorization();

// 健康检查
app.MapGet("/health", async (MySqlConnection db) =>
{
    var mysqlHealthy = false;
    try
    {
        await db.OpenAsync();
        using var cmd = new MySqlCommand("SELECT 1", db);
        await cmd.ExecuteScalarAsync();
        mysqlHealthy = true;
    }
    catch { }

    var status = mysqlHealthy ? "healthy" : "unhealthy";
    var httpStatus = mysqlHealthy ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable;

    return Results.Json(new
    {
        status,
        service = "WO.Property.AuthService",
        version = "2.2.0-multi-tenant",
        timestamp = DateTime.UtcNow,
        database = "center_db",
        multiTenant = true
    }, statusCode: httpStatus);
}).AllowAnonymous();

// ========== 登录接口（Phase 0 改造：支持 tenantCode）==========

app.MapPost("/api/auth/login", async (LoginRequest request, MySqlConnection db) =>
{
    var config = builder.Configuration.GetSection("LoginProtection").Get<LoginProtectionConfig>()
        ?? new LoginProtectionConfig();
    var ip = GetClientIpFromRequest(request);
    var now = DateTime.UtcNow;

    // 检查用户是否被锁定
    string lockKey = $"lockout:{request.Username}:{request.TenantCode}".ToLowerInvariant();
    if (loginFailures.TryGetValue(lockKey, out var userRecord))
    {
        if (now < userRecord.LockoutEnd)
        {
            var remainingSeconds = (int)(userRecord.LockoutEnd.Value - now).TotalSeconds;
            return Results.Json(new
            {
                success = false,
                error = "AccountLocked",
                code = "ACCOUNT_LOCKED",
                message = $"账户已锁定，请在 {remainingSeconds / 60 + 1} 分钟后重试",
                lockedUntil = userRecord.LockoutEnd.Value.ToString("O"),
                attemptsRemaining = 0
            }, statusCode: StatusCodes.Status423Locked);
        }
        else
        {
            loginFailures.TryRemove(lockKey, out _);
        }
    }

    await db.OpenAsync();

    // 1. 验证租户存在且正常
    using var tenantCmd = new MySqlCommand(@"
        SELECT id, tenant_code, tenant_name, db_name, status
        FROM tenants WHERE tenant_code = @TenantCode", db);
    tenantCmd.Parameters.AddWithValue("@TenantCode", request.TenantCode);

    int tenantId = 0;
    string tenantCode = "";
    string tenantDbName = "";
    using (var tenantReader = await tenantCmd.ExecuteReaderAsync())
    {
        if (!await tenantReader.ReadAsync())
        {
            return Results.BadRequest(new { success = false, message = "租户不存在或已被禁用" });
        }
        tenantId = tenantReader.GetInt32("id");
        tenantCode = tenantReader.GetString("tenant_code");
        var tenantName = tenantReader.GetString("tenant_name");
        tenantDbName = tenantReader.GetString("db_name");
        var tenantStatus = tenantReader.GetString("status");
        if (tenantStatus != "Active")
        {
            return Results.BadRequest(new { success = false, message = "租户已被禁用" });
        }
    }

    // 2. 查询用户（带 tenant_id 条件）
    using var userCmd = new MySqlCommand(@"
        SELECT id, username, full_name, email, phone, password_hash, role, status, tenant_id
        FROM users WHERE Username = @Username AND tenant_id = @TenantId", db);
    userCmd.Parameters.AddWithValue("@Username", request.Username);
    userCmd.Parameters.AddWithValue("@TenantId", tenantId);

    int userId = 0;
    string passwordHash = "";
    string fullName = "";
    string? email = null;
    string? phone = null;
    string role = "";
    string status = "";
    int userTenantId = 0;

    using (var userReader = await userCmd.ExecuteReaderAsync())
    {
        if (!await userReader.ReadAsync())
        {
            // 用户不存在，但仍然记录失败（防止枚举攻击）
            RecordLoginFailure(request.Username, ip, config, loginFailures, request.TenantCode);
            return Results.Unauthorized();
        }

        userId = userReader.GetInt32("id");
        var username = userReader.GetString("username");
        fullName = userReader.GetString("full_name");
        int emailIdx = userReader.GetOrdinal("email");
        int phoneIdx = userReader.GetOrdinal("phone");
        email = userReader.IsDBNull(emailIdx) ? null : userReader.GetString(emailIdx);
        phone = userReader.IsDBNull(phoneIdx) ? null : userReader.GetString(phoneIdx);
        passwordHash = userReader.GetString("password_hash");
        role = userReader.GetString("role");
        status = userReader.GetString("status");
        userTenantId = userReader.GetInt32("tenant_id");
    }

    if (!BCrypt.Net.BCrypt.Verify(request.Password, passwordHash))
    {
        RecordLoginFailure(request.Username, ip, config, loginFailures, request.TenantCode);
        var remaining = config.MaxFailuresBeforeLockout - (loginFailures.TryGetValue(lockKey, out var rec) ? rec.FailureCount : 0);
        return Results.Unauthorized();
    }

    // 登录成功，清除失败记录
    loginFailures.TryRemove(lockKey, out _);

    if (status != "Active")
        return Results.BadRequest(new { success = false, message = "用户账户已被禁用" });

    // 3. 查询用户所属项目
    var projectIds = new List<int>();
    var projects = new List<object>();

    using (var projectCmd = new MySqlCommand(@"
        SELECT up.project_id, p.project_name, p.project_code
        FROM user_projects up
        JOIN projects p ON up.project_id = p.id
        WHERE up.user_id = @UserId", db))
    {
        projectCmd.Parameters.AddWithValue("@UserId", userId);
        using var projectReader = await projectCmd.ExecuteReaderAsync();
        while (await projectReader.ReadAsync())
        {
            var pid = projectReader.GetInt32("project_id");
            projectIds.Add(pid);
            projects.Add(new
            {
                id = pid,
                name = projectReader.GetString("project_name"),
                code = projectReader.GetString("project_code")
            });
        }
    }

    // 4. 生成 JWT（新增 tenant 相关字段）
    var token = GenerateJwtToken(userId, request.Username, fullName, email, role, status, tenantId, tenantCode, projectIds);

    return Results.Ok(new
    {
        success = true,
        message = "登录成功",
        data = new
        {
            token,
            expiresAt = DateTime.UtcNow.AddDays(7),
            tenantId,
            tenantCode,
            projectIds,
            projects,
            user = new
            {
                id = userId,
                username = request.Username,
                fullName,
                email,
                phone,
                role
            }
        }
    });
});

// 注册接口（保持简单，无租户概念）
app.MapPost("/api/auth/register", async (RegisterRequest request, MySqlConnection db) =>
{
    await db.OpenAsync();
    using var checkCmd = new MySqlCommand("SELECT COUNT(*) FROM users WHERE Username = @Username", db);
    checkCmd.Parameters.AddWithValue("@Username", request.Username);
    var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
    if (count > 0)
        return Results.BadRequest(new { Success = false, Message = "用户名已存在" });

    var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

    using var insertCmd = new MySqlCommand(@"
        INSERT INTO users (Username, FullName, Email, Phone, PasswordHash, Role, Status, CreatedAt, TenantId)
        VALUES (@Username, @FullName, @Email, @Phone, @PasswordHash, 'User', 'Active', @CreatedAt, 0)", db);
    insertCmd.Parameters.AddWithValue("@Username", request.Username);
    insertCmd.Parameters.AddWithValue("@FullName", request.FullName);
    insertCmd.Parameters.AddWithValue("@Email", (object?)request.Email ?? DBNull.Value);
    insertCmd.Parameters.AddWithValue("@Phone", (object?)request.Phone ?? DBNull.Value);
    insertCmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
    insertCmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
    await insertCmd.ExecuteNonQueryAsync();

    using var lastIdCmd = new MySqlCommand("SELECT LAST_INSERT_ID()", db);
    var userId = Convert.ToInt32(await lastIdCmd.ExecuteScalarAsync());

    return Results.Ok(new { Success = true, Message = "注册成功", UserId = userId });
});

// 注销
app.MapPost("/api/auth/logout", [Authorize] async (HttpContext context, MySqlConnection db) =>
{
    await db.OpenAsync();
    return Results.Ok(new { Success = true, Message = "已退出登录" });
});

app.Run();

// ============================================================
// 辅助方法
// ============================================================

void RecordLoginFailure(string username, string ip, LoginProtectionConfig config, ConcurrentDictionary<string, LoginFailureRecord> dict, string tenantCode)
{
    var lockKey = $"lockout:{username}:{tenantCode}".ToLowerInvariant();
    var now = DateTime.UtcNow;

    dict.AddOrUpdate(lockKey,
        _ => new LoginFailureRecord
        {
            Username = username,
            Ip = ip,
            AttemptTime = now,
            FailureCount = 1,
            LockoutEnd = now.AddMinutes(config.LockoutMinutes)
        },
        (_, existing) =>
        {
            existing.FailureCount++;
            existing.AttemptTime = now;
            if (existing.FailureCount >= config.MaxFailuresBeforeLockout)
            {
                existing.LockoutEnd = now.AddMinutes(config.LockoutMinutes);
            }
            return existing;
        });
}

string GetClientIp(HttpContext context)
{
    var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
    if (!string.IsNullOrEmpty(forwarded))
    {
        return forwarded.Split(',')[0].Trim();
    }
    return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}

string GetClientIpFromRequest(LoginRequest request) => "unknown";

// 生成JWT令牌（Phase 0 新增 tenant_id / tenant_code / project_ids）
string GenerateJwtToken(int userId, string username, string fullName, string? email, string role, string status,
    int tenantId, string tenantCode, List<int> projectIds)
{
    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
        new Claim(ClaimTypes.Name, username),
        new Claim(ClaimTypes.Email, email ?? ""),
        new Claim(ClaimTypes.GivenName, fullName),
        new Claim(ClaimTypes.Role, role),
        new Claim("Status", status),
        // ===== Phase 0 新增租户字段 =====
        new Claim("tenant_id", tenantId.ToString()),
        new Claim("tenant_code", tenantCode),
        new Claim("project_ids", string.Join(",", projectIds))
    };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: "wo-property-unified-auth",
        audience: "wo-property-services",
        claims: claims,
        expires: DateTime.UtcNow.AddDays(7),
        signingCredentials: credentials
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}

// ============================================================
// 配置类
// ============================================================

public class LoginProtectionConfig
{
    public bool Enabled { get; set; } = true;
    public int MaxFailuresBeforeLockout { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 15;
    public int MaxFailuresBeforeBan { get; set; } = 20;
    public int BanDurationSeconds { get; set; } = 1800;
}

public class LoginFailureRecord
{
    public string? Username { get; set; }
    public string Ip { get; set; } = "";
    public DateTime AttemptTime { get; set; }
    public int FailureCount { get; set; }
    public DateTime? LockoutEnd { get; set; }
}

// 请求模型（Phase 0 新增 TenantCode）
public class RegisterRequest
{
    [Required, MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress, MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 租户编码（必填）
    /// </summary>
    [Required]
    public string TenantCode { get; set; } = string.Empty;
}