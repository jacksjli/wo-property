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

var builder = WebApplication.CreateBuilder(args);

// 配置端口 - 使用5006端口
builder.WebHost.UseUrls("http://0.0.0.0:5006");

// 获取数据库连接字符串
var dbConnectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    ?? "Server=localhost;Port=3306;Database=wo_property;User=woproperty;Password=WOProperty2026!;CharSet=utf8mb4;Pooling=true;Minimum Pool Size=2;Maximum Pool Size=20;Connection Timeout=10;Connection Idle Timeout=60;Default Command Timeout=30;";

// 注册 MySQL 连接
builder.Services.AddScoped<MySqlConnection>(_ =>
    new MySqlConnection(dbConnectionString));

// JWT 密钥（从环境变量 JWT_SECRET_KEY 读取）
var jwtSecretKey = JwtHelper.GetSecretKey();

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
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSecretKey))
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

// === 登录防护配置（从 appsettings.json 读取） ===
var loginProtectionConfig = builder.Configuration.GetSection("LoginProtection").Get<LoginProtectionConfig>()
    ?? new LoginProtectionConfig();

// === 登录防护内存存储（生产环境建议用 Redis） ===
var loginFailures = new ConcurrentDictionary<string, LoginFailureRecord>();
var ipFailures = new ConcurrentDictionary<string, LoginFailureRecord>();

var app = builder.Build();

// 初始化数据库
await InitializeDatabaseAsync();

// 配置中间件
app.UseCors("AllowFrontend");

// 登录防护中间件
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
    var method = context.Request.Method;

    // 只拦截登录请求
    if (path.Contains("/api/auth/login") && method == "POST")
    {
        var ip = GetClientIp(context);
        var now = DateTime.UtcNow;

        // 检查 IP 是否被封禁
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
                // 解封
                ipFailures.TryRemove(ip, out _);
            }
        }

        // 动态读取配置（支持运行时更新）
        var config = builder.Configuration.GetSection("LoginProtection").Get<LoginProtectionConfig>()
            ?? new LoginProtectionConfig();

        // 检查频率限制（同一 IP 短时间多次尝试）
        var recentFailures = ipFailures.Values
            .Where(r => r.Ip == ip && (now - r.AttemptTime).TotalSeconds < config.BanDurationSeconds)
            .Count();

        if (recentFailures >= config.MaxFailuresBeforeBan)
        {
            // 封禁该 IP
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

    var response = new
    {
        status,
        service = "WO.Property.AuthService",
        version = "2.1.0",
        timestamp = DateTime.UtcNow,
        dependencies = new Dictionary<string, object>
        {
            ["mysql"] = new { status = mysqlHealthy ? "healthy" : "unhealthy" }
        },
        loginProtection = new
        {
            enabled = loginProtectionConfig.Enabled,
            maxFailuresBeforeLockout = loginProtectionConfig.MaxFailuresBeforeLockout,
            lockoutMinutes = loginProtectionConfig.LockoutMinutes,
            banAfterNAttempts = loginProtectionConfig.MaxFailuresBeforeBan,
            banDurationMinutes = loginProtectionConfig.BanDurationSeconds / 60
        }
    };

    return Results.Json(response, statusCode: httpStatus);
}).AllowAnonymous();

// 用户注册
app.MapPost("/api/auth/register", async (RegisterRequest request, MySqlConnection db) =>
{
    await db.OpenAsync();
    using var checkCmd = new MySqlCommand("SELECT COUNT(*) FROM users WHERE Username = @Username", db);
    checkCmd.Parameters.AddWithValue("@Username", request.Username);
    var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
    if (count > 0)
        return Results.BadRequest(new { Success = false, Message = "用户名已存在" });

    // 密码加密
    var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

    using var insertCmd = new MySqlCommand(@"
        INSERT INTO users (Username, FullName, Email, Phone, PasswordHash, Role, Status, CreatedAt)
        VALUES (@Username, @FullName, @Email, @Phone, @PasswordHash, 'User', 'Active', @CreatedAt)", db);
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

// 用户登录（带登录防护）
app.MapPost("/api/auth/login", async (LoginRequest request, MySqlConnection db) =>
{
    var config = builder.Configuration.GetSection("LoginProtection").Get<LoginProtectionConfig>()
        ?? new LoginProtectionConfig();
    var ip = GetClientIpFromRequest(request);
    var now = DateTime.UtcNow;

    // 检查用户是否被锁定
    string lockKey = $"lockout:{request.Username}".ToLowerInvariant();
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
            // 解除锁定
            loginFailures.TryRemove(lockKey, out _);
        }
    }

    await db.OpenAsync();
    using var cmd = new MySqlCommand(@"
        SELECT Id, Username, FullName, Email, Phone, PasswordHash, Role, Status, CreatedAt
        FROM users WHERE Username = @Username", db);
    cmd.Parameters.AddWithValue("@Username", request.Username);

    using var reader = await cmd.ExecuteReaderAsync();
    if (!await reader.ReadAsync())
    {
        // 用户不存在，但仍然记录失败（防止枚举攻击）
        RecordLoginFailure(request.Username, ip, config, loginFailures);
        return Results.Unauthorized();
    }

    int idIdx = reader.GetOrdinal("Id");
    int usernameIdx = reader.GetOrdinal("Username");
    int fullNameIdx = reader.GetOrdinal("FullName");
    int emailIdx = reader.GetOrdinal("Email");
    int phoneIdx = reader.GetOrdinal("Phone");
    int passwordHashIdx = reader.GetOrdinal("PasswordHash");
    int roleIdx = reader.GetOrdinal("Role");
    int statusIdx = reader.GetOrdinal("Status");
    int createdAtIdx = reader.GetOrdinal("CreatedAt");

    var user = new
    {
        Id = reader.GetInt32(idIdx),
        Username = reader.GetString(usernameIdx),
        FullName = reader.GetString(fullNameIdx),
        Email = reader.IsDBNull(emailIdx) ? null : reader.GetString(emailIdx),
        Phone = reader.IsDBNull(phoneIdx) ? null : reader.GetString(phoneIdx),
        PasswordHash = reader.GetString(passwordHashIdx),
        Role = reader.GetString(roleIdx),
        Status = reader.GetString(statusIdx),
        CreatedAt = reader.GetDateTime(createdAtIdx)
    };

    if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
    {
        // 密码错误，记录失败
        RecordLoginFailure(request.Username, ip, config, loginFailures);

        var remaining = config.MaxFailuresBeforeLockout - (loginFailures.TryGetValue(lockKey, out var rec) ? rec.FailureCount : 0);
        return Results.Unauthorized();
    }

    // 登录成功，清除失败记录
    loginFailures.TryRemove(lockKey, out _);

    if (user.Status != "Active")
        return Results.BadRequest(new { Success = false, Message = "用户账户已被禁用" });

    var token = GenerateJwtToken(user);
    var refreshToken = GenerateRefreshToken();

    // 存储 RefreshToken
    await reader.CloseAsync();
    using var insertCmd = new MySqlCommand(@"
        INSERT INTO refresh_tokens (token, user_id, expires_at, created_at)
        VALUES (@Token, @UserId, @ExpiresAt, @CreatedAt)", db);
    insertCmd.Parameters.AddWithValue("@Token", refreshToken);
    insertCmd.Parameters.AddWithValue("@UserId", user.Id);
    insertCmd.Parameters.AddWithValue("@ExpiresAt", DateTime.UtcNow.AddDays(7));
    insertCmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
    await insertCmd.ExecuteNonQueryAsync();

    return Results.Ok(new
    {
        Success = true,
        Message = "登录成功",
        Data = new
        {
            Token = token,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(2),
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7),
            User = new
            {
                user.Id,
                user.Username,
                user.FullName,
                user.Email,
                user.Phone,
                user.Role,
                user.Status
            }
        }
    });
});

// 获取当前用户信息
app.MapGet("/api/auth/me", [Authorize] async (HttpContext context, MySqlConnection db) =>
{
    await db.OpenAsync();
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var id))
        return Results.Unauthorized();

    using var cmd = new MySqlCommand(@"
        SELECT Id, Username, FullName, Email, Phone, Role, Status, CreatedAt
        FROM users WHERE Id = @Id", db);
    cmd.Parameters.AddWithValue("@Id", id);

    using var reader = await cmd.ExecuteReaderAsync();
    if (!await reader.ReadAsync())
        return Results.NotFound(new { Success = false, Message = "用户不存在" });

    int idIdx = reader.GetOrdinal("Id");
    int usernameIdx = reader.GetOrdinal("Username");
    int fullNameIdx = reader.GetOrdinal("FullName");
    int emailIdx = reader.GetOrdinal("Email");
    int phoneIdx = reader.GetOrdinal("Phone");
    int roleIdx = reader.GetOrdinal("Role");
    int statusIdx = reader.GetOrdinal("Status");
    int createdAtIdx = reader.GetOrdinal("CreatedAt");

    var user = new
    {
        Id = reader.GetInt32(idIdx),
        Username = reader.GetString(usernameIdx),
        FullName = reader.GetString(fullNameIdx),
        Email = reader.IsDBNull(emailIdx) ? null : reader.GetString(emailIdx),
        Phone = reader.IsDBNull(phoneIdx) ? null : reader.GetString(phoneIdx),
        Role = reader.GetString(roleIdx),
        Status = reader.GetString(statusIdx),
        CreatedAt = reader.GetDateTime(createdAtIdx)
    };

    return Results.Ok(new
    {
        Success = true,
        Data = user
    });
});

// Refresh Token 续期
app.MapPost("/api/auth/refresh", async (RefreshTokenRequest request, MySqlConnection db) =>
{
    await db.OpenAsync();

    // 查询 RefreshToken
    using var tokenCmd = new MySqlCommand(@"
        SELECT rt.id, rt.token, rt.user_id, rt.expires_at, rt.created_at,
               u.Id as uid, u.Username, u.FullName, u.Email, u.Phone, u.Role, u.Status
        FROM refresh_tokens rt
        JOIN users u ON rt.user_id = u.Id
        WHERE rt.token = @Token", db);
    tokenCmd.Parameters.AddWithValue("@Token", request.RefreshToken);

    using var tokenReader = await tokenCmd.ExecuteReaderAsync();
    if (!await tokenReader.ReadAsync())
        return Results.BadRequest(new { Success = false, Message = "无效的 Refresh Token" });

    int expiresAtIdx = tokenReader.GetOrdinal("expires_at");
    int uidIdx = tokenReader.GetOrdinal("uid");
    int usernameIdx = tokenReader.GetOrdinal("Username");
    int fullNameIdx = tokenReader.GetOrdinal("FullName");
    int emailIdx = tokenReader.GetOrdinal("Email");
    int phoneIdx = tokenReader.GetOrdinal("Phone");
    int roleIdx = tokenReader.GetOrdinal("Role");
    int statusIdx = tokenReader.GetOrdinal("Status");

    var expiresAt = tokenReader.GetDateTime(expiresAtIdx);
    if (expiresAt < DateTime.UtcNow)
    {
        return Results.BadRequest(new { Success = false, Message = "Refresh Token 已过期" });
    }

    var user = new
    {
        Id = tokenReader.GetInt32(uidIdx),
        Username = tokenReader.GetString(usernameIdx),
        FullName = tokenReader.GetString(fullNameIdx),
        Email = tokenReader.IsDBNull(emailIdx) ? null : reader.GetString(emailIdx),
        Phone = tokenReader.IsDBNull(phoneIdx) ? null : reader.GetString(phoneIdx),
        Role = tokenReader.GetString(roleIdx),
        Status = tokenReader.GetString(statusIdx)
    };

    if (user.Status != "Active")
        return Results.BadRequest(new { Success = false, Message = "用户账户不可用" });

    await tokenReader.CloseAsync();

    // 生成新 Access Token
    var newToken = GenerateJwtToken(user);

    // 生成新 Refresh Token（轮换）
    var newRefreshToken = GenerateRefreshToken();

    // 删除旧 Refresh Token
    using var deleteCmd = new MySqlCommand("DELETE FROM refresh_tokens WHERE token = @Token", db);
    deleteCmd.Parameters.AddWithValue("@Token", request.RefreshToken);
    await deleteCmd.ExecuteNonQueryAsync();

    // 添加新 Refresh Token
    using var insertCmd = new MySqlCommand(@"
        INSERT INTO refresh_tokens (token, user_id, expires_at, created_at)
        VALUES (@Token, @UserId, @ExpiresAt, @CreatedAt)", db);
    insertCmd.Parameters.AddWithValue("@Token", newRefreshToken);
    insertCmd.Parameters.AddWithValue("@UserId", user.Id);
    insertCmd.Parameters.AddWithValue("@ExpiresAt", DateTime.UtcNow.AddDays(7));
    insertCmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
    await insertCmd.ExecuteNonQueryAsync();

    return Results.Ok(new
    {
        Success = true,
        Data = new
        {
            Token = newToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(2),
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7),
            User = new
            {
                user.Id,
                user.Username,
                user.FullName,
                user.Email,
                user.Phone,
                user.Role,
                user.Status
            }
        }
    });
});

// 注销
app.MapPost("/api/auth/logout", [Authorize] async (HttpContext context, MySqlConnection db) =>
{
    await db.OpenAsync();
    return Results.Ok(new { Success = true, Message = "已退出登录" });
});

// ============================================================
// 登录防护 API（供前端调用查询状态）
// ============================================================

// 查询账户登录状态（是否被锁定）
app.MapGet("/api/auth/login-status/{username}", (string username) =>
{
    var lockKey = $"lockout:{username}".ToLowerInvariant();
    if (loginFailures.TryGetValue(lockKey, out var record))
    {
        if (DateTime.UtcNow < record.LockoutEnd)
        {
            return Results.Json(new
            {
                locked = true,
                lockedUntil = record.LockoutEnd.Value.ToString("O"),
                remainingSeconds = (int)(record.LockoutEnd.Value - DateTime.UtcNow).TotalSeconds
            });
        }
    }
    return Results.Json(new { locked = false });
}).AllowAnonymous();

// 获取验证码（滑动验证）接口（预留）
app.MapGet("/api/auth/captcha", () =>
{
    // TODO: 接入滑动验证（腾讯防水墙、顶象等）
    return Results.Json(new
    {
        captchaId = Guid.NewGuid().ToString("N"),
        captchaType = "slider", // 或 "float"、"click"
        expiresAt = DateTime.UtcNow.AddMinutes(5).ToString("O")
    });
}).AllowAnonymous();

// 验证验证码（预留）
app.MapPost("/api/auth/verify-captcha", (VerifyCaptchaRequest request) =>
{
    // TODO: 接入滑动验证服务
    return Results.Ok(new
    {
        success = true,
        valid = true
    });
}).AllowAnonymous();

app.Run();

// ============================================================
// 辅助方法
// ============================================================

void RecordLoginFailure(string username, string ip, LoginProtectionConfig config, ConcurrentDictionary<string, LoginFailureRecord> dict)
{
    var lockKey = $"lockout:{username}".ToLowerInvariant();
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
    // 优先取 X-Forwarded-For（反向代理）
    var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
    if (!string.IsNullOrEmpty(forwarded))
    {
        return forwarded.Split(',')[0].Trim();
    }
    return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}

string GetClientIpFromRequest(LoginRequest request)
{
    // 登录请求中没有 IP 信息，需要从 HttpContext 获取
    // 这里通过静态方式获取，实际在 middleware 中已经处理
    return "unknown";
}

// 数据库初始化
async Task InitializeDatabaseAsync()
{
    var dbConnectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
        ?? "Server=localhost;Port=3306;Database=wo_property;User=woproperty;Password=WOProperty2026!;CharSet=utf8mb4;Pooling=true;Minimum Pool Size=2;Maximum Pool Size=10;Connection Timeout=10;Connection Idle Timeout=60;Default Command Timeout=30;";

    using (var connection = new MySqlConnection(dbConnectionString))
    {
        await connection.OpenAsync();

        var createUsersSql = @"
CREATE TABLE IF NOT EXISTS users (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    Username VARCHAR(50) NOT NULL UNIQUE,
    FullName VARCHAR(100) NOT NULL,
    Email VARCHAR(100),
    Phone VARCHAR(20),
    PasswordHash VARCHAR(200) NOT NULL,
    Role VARCHAR(20) NOT NULL DEFAULT 'User',
    Status VARCHAR(20) NOT NULL DEFAULT 'Active',
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
)";
        using (var cmd = new MySqlCommand(createUsersSql, connection)) { await cmd.ExecuteNonQueryAsync(); }

        var createTokensSql = @"
CREATE TABLE IF NOT EXISTS refresh_tokens (
    id INT PRIMARY KEY AUTO_INCREMENT,
    token VARCHAR(255) NOT NULL UNIQUE,
    user_id INT NOT NULL,
    expires_at TIMESTAMP NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_user FOREIGN KEY (user_id) REFERENCES users(Id) ON DELETE CASCADE
)";
        using (var cmd = new MySqlCommand(createTokensSql, connection)) { await cmd.ExecuteNonQueryAsync(); }

        var indexSqls = new[] {
            "CREATE INDEX idx_rt_token ON refresh_tokens(token)",
            "CREATE INDEX idx_rt_user_id ON refresh_tokens(user_id)",
            "CREATE INDEX idx_rt_expires ON refresh_tokens(expires_at)"
        };
        foreach (var idxSql in indexSqls)
        {
            try { using var idxCmd = new MySqlCommand(idxSql, connection); await idxCmd.ExecuteNonQueryAsync(); }
            catch (MySqlException ex) when (ex.Number == 1061) { /* 索引已存在 */ }
        }

        var checkCmd = new MySqlCommand("SELECT COUNT(*) FROM users", connection);
        var userCount = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

        if (userCount == 0)
        {
            var users = new[]
            {
                ("admin", "系统管理员", "admin@wo-property.com", "13800138000", BCrypt.Net.BCrypt.HashPassword("Admin@123"), "Administrator"),
                ("tech", "技术人员", "tech@wo-property.com", "13800138001", BCrypt.Net.BCrypt.HashPassword("Tech@123"), "Technician"),
                ("user", "普通用户", "user@wo-property.com", "13800138002", BCrypt.Net.BCrypt.HashPassword("User@123"), "User")
            };

            foreach (var (username, fullName, email, phone, passwordHash, role) in users)
            {
                using var insertCmd = new MySqlCommand(@"
                    INSERT INTO users (Username, FullName, Email, Phone, PasswordHash, Role, Status, CreatedAt)
                    VALUES (@Username, @FullName, @Email, @Phone, @PasswordHash, @Role, 'Active', @CreatedAt)", connection);
                insertCmd.Parameters.AddWithValue("@Username", username);
                insertCmd.Parameters.AddWithValue("@FullName", fullName);
                insertCmd.Parameters.AddWithValue("@Email", email);
                insertCmd.Parameters.AddWithValue("@Phone", phone);
                insertCmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
                insertCmd.Parameters.AddWithValue("@Role", role);
                insertCmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
                await insertCmd.ExecuteNonQueryAsync();
            }
            Console.WriteLine("用户初始化成功");
        }
    }
}

// 生成JWT令牌
string GenerateJwtToken(dynamic user)
{
    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Email, user.Email ?? ""),
        new Claim(ClaimTypes.GivenName, user.FullName),
        new Claim(ClaimTypes.Role, user.Role),
        new Claim("Status", user.Status)
    };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: "wo-property-unified-auth",
        audience = "wo-property-services",
        claims: claims,
        expires: DateTime.UtcNow.AddHours(2),
        signingCredentials: credentials
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}

// 生成 RefreshToken（随机字符串）
string GenerateRefreshToken()
{
    return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
        .Replace("+", "-")
        .Replace("/", "_")
        .TrimEnd('=');
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

// 请求模型
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
}

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

public class VerifyCaptchaRequest
{
    public string CaptchaId { get; set; } = string.Empty;
    public string CaptchaToken { get; set; } = string.Empty;
    public string? ExtraData { get; set; }
}