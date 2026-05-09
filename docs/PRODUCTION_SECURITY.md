# WO-Property 生产级 JWT 鉴权与权限矩阵设计

> **文档版本**: v1.0
> **编写日期**: 2026-05-09
> **状态**: 设计中（待实现）
> **负责人**: 安全工程师（子代理）

---

## 一、现状分析

### 1.1 现有 AuthService (5006) 的能力

| 功能 | 状态 | 说明 |
|------|------|------|
| 用户注册/登录 | ✅ 已实现 | BCrypt 密码存储 |
| JWT Access Token | ✅ 已实现 | HS256，2小时有效期 |
| Refresh Token 轮换 | ✅ 已实现 | 7天有效期，每次刷新删除旧token |
| 基础角色声明 | ✅ 已实现 | Role claim 在 JWT 中 |
| Token 黑名单 | ❌ 缺失 | 登出后旧 token 仍可用 |
| API 级权限控制 | ❌ 缺失 | 无基于角色的路由过滤 |
| 审计日志 | ❌ 缺失 | 敏感操作无记录 |

### 1.2 现有 APIGateway 的能力

| 功能 | 状态 | 说明 |
|------|------|------|
| JWT 验证 | ✅ 已实现 | 验证签名、Issuer、Audience |
| 全局限流 | ✅ 已实现 | 1000 req/s 全局 + 100 req/s per IP |
| 路由转发 | ✅ 已实现 | YARP 反向代理 |
| 角色授权 | ❌ 缺失 | Authorization policy 未配置 |
| 黑名单校验 | ❌ 缺失 | 未对接黑名单服务 |

---

## 二、JWT 鉴权体系设计

### 2.1 Token 生命周期

```
┌─────────────────────────────────────────────────────────────────┐
│                        用户登录                                  │
│                    POST /api/auth/login                          │
└─────────────────────────┬───────────────────────────────────────┘
                          │
                          ▼
               ┌─────────────────────────┐
               │ 验证用户名/密码 (BCrypt)  │
               └───────────┬─────────────┘
                          │ OK
                          ▼
          ┌────────────────────────────────────────┐
          │  颁发 Access Token (JWT)                │
          │  - sub: user_id                        │
          │  - role: 当前角色                        │
          │  - jti: 唯一标识符 (用于黑名单)           │
          │  - exp: now + 15min                     │
          │  - nbf: now                             │
          └────────────────┬───────────────────────┘
                           │
                           ▼
          ┌────────────────────────────────────────┐
          │  颁发 Refresh Token (随机字符串)        │
          │  - 存储至 MySQL: refresh_tokens 表       │
          │  - expires_at: now + 7days              │
          │  - user_id, device_id (可选)            │
          └────────────────┬───────────────────────┘
                           │
          ┌────────────────┴───────────────────────┐
          │           返回给客户端                   │
          │  { token, refreshToken, expiresAt }    │
          └────────────────────────────────────────┘
```

### 2.2 Token 配置参数

| 参数 | 值 | 说明 |
|------|-----|------|
| Access Token 有效期 | **15 分钟** | 短期token，频繁刷新 |
| Refresh Token 有效期 | **7 天** | 长期凭证，存储在DB |
| Token 算法 | HS256 | HMAC-SHA-256 |
| 时钟偏差 (ClockSkew) | 0 秒 | 严格验证 |
| Token Issuer | `wo-property-unified-auth` | 保持不变 |
| Token Audience | `wo-property-services` | 保持不变 |
| 黑名单有效期 | 与 Access Token 等长 | 15分钟 |

### 2.3 Access Token 结构（JWT Payload）

```json
{
  "sub": "123",
  "jti": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "role": "property_manager",
  "username": "zhangsan",
  "fullName": "张三",
  "status": "Active",
  "iss": "wo-property-unified-auth",
  "aud": "wo-property-services",
  "iat": 1715270400,
  "nbf": 1715270400,
  "exp": 1715271300
}
```

> **注意**：增加了 `jti`（JWT ID）字段，这是黑名单的关键标识。

### 2.4 Token 刷新流程

```
客户端                              AuthService                         MySQL
  │                                     │                              │
  │  POST /api/auth/refresh             │                              │
  │  { refreshToken: "xxx" }            │                              │
  │────────────────────────────────────►│                              │
  │                                     │  查询 refresh_tokens 表       │
  │                                     │  WHERE token = ?             │
  │                                     │────────────────────────────►│
  │                                     │◄─────────────────────────────│
  │                                     │  验证：                       │
  │                                     │  - token 存在                │
  │                                     │  - 未过期                     │
  │                                     │  - 用户状态 Active           │
  │                                     │                              │
  │                                     │  删除旧 RefreshToken         │
  │                                     │  DELETE FROM refresh_tokens  │
  │                                     │  WHERE token = ?             │
  │                                     │────────────────────────────►│
  │                                     │                              │
  │                                     │  生成新 Access Token          │
  │                                     │  (jti 为新值)                 │
  │                                     │                              │
  │                                     │  生成新 RefreshToken         │
  │                                     │  INSERT INTO refresh_tokens  │
  │                                     │────────────────────────────►│
  │                                     │                              │
  │  返回:                              │                              │
  │  { token, refreshToken,             │                              │
  │    expiresAt, user }                │                              │
  │◄────────────────────────────────────│                              │
```

#### 刷新时机规则

| 场景 | 是否刷新 RefreshToken |
|------|----------------------|
| Access Token 即将过期（< 5分钟） | ✅ 刷新（轮换） |
| Access Token 还有余量（> 5分钟） | ❌ 不刷新，减少 DB 写 |
| Refresh Token 已接近过期（< 1天） | ✅ 刷新（轮换） |
| 用户主动"记住我"场景 | ✅ 刷新 |

> **关键**：每次 refresh 必须删除旧 RefreshToken 并创建新的（防止 token 被盗后重复使用）。

### 2.5 黑名单机制

#### 黑名单类型

| 类型 | 触发条件 | 有效期 |
|------|----------|--------|
| 单 Token 黑名单 | 登出（Logout） | 至 Access Token 原始过期时间 |
| 用户全量黑名单 | 修改密码、账户禁用 | 持续至下次登录 |
| Token 序列号黑名单 | RefreshToken 被使用后 | 永久（标记为已使用） |

#### 黑名单存储（MySQL 表）

```sql
CREATE TABLE token_blacklist (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    token_id VARCHAR(64) NOT NULL COMMENT 'JWT 的 jti 或 RefreshToken 值',
    blacklist_type ENUM('access_token', 'refresh_token', 'user_all') NOT NULL,
    user_id INT NULL COMMENT '用户ID（user_all类型时必填）',
    reason VARCHAR(255) NOT NULL COMMENT '拉黑原因',
    expires_at TIMESTAMP NULL COMMENT '黑名单过期时间（NULL表示永久）',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_token_id (token_id),
    INDEX idx_user_id (user_id),
    INDEX idx_expires (blacklist_type, expires_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

#### 登出流程（Logout）

```csharp
// POST /api/auth/logout
// 1. 从 JWT 中提取 jti 和 user_id
// 2. 将 jti 加入黑名单（blacklist_type='access_token'，expires_at = token原始过期时间）
// 3. 删除用户所有 RefreshToken（DELETE FROM refresh_tokens WHERE user_id = ?）
// 4. 若需要，可将用户加入全量黑名单（password_changed 等场景）
```

#### APIGateway 黑名单校验中间件

```csharp
// 在 APIGateway 的 JWT Events 中增加黑名单校验
options.Events = new JwtBearerEvents
{
    OnTokenValidated = context =>
    {
        var jti = context.Principal?.FindFirst("jti")?.Value;
        if (!string.IsNullOrEmpty(jti))
        {
            // 查询黑名单
            var isBlacklisted = await CheckBlacklistAsync(jti, "access_token");
            if (isBlacklisted)
            {
                context.Fail("Token has been revoked");
                return;
            }
        }
        return;
    }
};
```

> **性能优化**：生产环境应使用 Redis 存储黑名单，而非每次查 MySQL。

### 2.6 密码修改后的安全处理

```csharp
// PUT /api/auth/change-password（需登录）
app.MapPut("/api/auth/change-password", [Authorize] async (HttpContext ctx, ChangePasswordRequest req, MySqlConnection db) =>
{
    var userId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    
    await db.OpenAsync();
    
    // 1. 验证旧密码
    using var checkCmd = new MySqlCommand("SELECT PasswordHash FROM users WHERE Id = @Id", db);
    checkCmd.Parameters.AddWithValue("@Id", userId);
    var hash = (await checkCmd.ExecuteScalarAsync()) as string;
    if (!BCrypt.Net.BCrypt.Verify(req.OldPassword, hash))
        return Results.BadRequest(new { Success = false, Message = "旧密码错误" });

    // 2. 更新密码
    var newHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
    using var updateCmd = new MySqlCommand("UPDATE users SET PasswordHash = @Hash WHERE Id = @Id", db);
    updateCmd.Parameters.AddWithValue("@Hash", newHash);
    updateCmd.Parameters.AddWithValue("@Id", userId);
    await updateCmd.ExecuteNonQueryAsync();

    // 3. 将该用户所有 RefreshToken 加入黑名单（删除）
    using var deleteRtCmd = new MySqlCommand("DELETE FROM refresh_tokens WHERE user_id = @UserId", db);
    deleteRtCmd.Parameters.AddWithValue("@UserId", userId);
    await deleteRtCmd.ExecuteNonQueryAsync();

    // 4. 将用户加入全量黑名单（基于 user_id）
    using var blacklistCmd = new MySqlCommand(@"
        INSERT INTO token_blacklist (token_id, blacklist_type, user_id, reason, expires_at)
        VALUES (@TokenId, 'user_all', @UserId, 'password_changed', NULL)", db);
    blacklistCmd.Parameters.AddWithValue("@TokenId", $"pwd_changed_{userId}_{DateTime.UtcNow.Ticks}");
    blacklistCmd.Parameters.AddWithValue("@UserId", userId);
    await blacklistCmd.ExecuteNonQueryAsync();

    // 5. 生成新的 AccessToken（包含新 jti）
    // 返回新 token，让用户无需重新登录
    
    return Results.Ok(new { Success = true, Message = "密码已修改，请使用新token" });
});
```

---

## 三、角色权限矩阵设计

### 3.1 角色定义

| 角色 | 中文名 | 说明 |
|------|--------|------|
| `admin` | 系统管理员 | 最高权限，系统配置 |
| `property_manager` | 物业经理 | 管辖范围内全部操作 |
| `operator` | 工作人员 | 执行工单、设备操作 |
| `resident` | 业主/住户 | 提交工单、查看公告 |

### 3.2 API 权限矩阵

> 格式：`GET` / `POST` / `PUT` / `DELETE` — 无标记表示该操作对角色不可用

| API 路径 | admin | property_manager | operator | resident |
|----------|:-----:|:-----------------:|:--------:|:--------:|
| **登录/注册** (无需认证) | | | | |
| `POST /api/auth/login` | — | — | — | — |
| `POST /api/auth/register` | — | — | — | — |
| **认证相关** (需认证) | | | | |
| `GET /api/auth/me` | ✅ | ✅ | ✅ | ✅ |
| `POST /api/auth/refresh` | ✅ | ✅ | ✅ | ✅ |
| `POST /api/auth/logout` | ✅ | ✅ | ✅ | ✅ |
| `PUT /api/auth/change-password` | ✅ | ✅ | ✅ | ✅ |
| **工单模块** | | | | |
| `GET /api/tickets` (列表) | ✅ | ✅(管辖范围) | ✅ | ✅(自己) |
| `GET /api/tickets/{id}` | ✅ | ✅ | ✅ | ✅ |
| `POST /api/tickets` (创建) | ✅ | ✅ | ✅ | ✅ |
| `PUT /api/tickets/{id}` (处理) | ✅ | ✅ | ✅ | ❌ |
| `PUT /api/tickets/{id}/close` | ✅ | ✅ | ✅ | ❌ |
| `DELETE /api/tickets/{id}` | ✅ | ✅ | ❌ | ❌ |
| `POST /api/tickets/batch-close` | ✅ | ✅ | ❌ | ❌ |
| **设备模块** | | | | |
| `GET /api/devices` | ✅ | ✅ | ✅ | ✅ |
| `GET /api/devices/{id}` | ✅ | ✅ | ✅ | ✅ |
| `POST /api/devices` | ✅ | ✅ | ❌ | ❌ |
| `PUT /api/devices/{id}` | ✅ | ✅ | ✅ | ❌ |
| `DELETE /api/devices/{id}` | ✅ | ✅ | ❌ | ❌ |
| `GET /api/devices/export` (数据导出) | ✅ | ✅ | ❌ | ❌ |
| **公告模块** | | | | |
| `GET /api/announcements` | ✅ | ✅ | ✅ | ✅ |
| `POST /api/announcements` | ✅ | ✅ | ❌ | ❌ |
| `PUT /api/announcements/{id}` | ✅ | ✅ | ❌ | ❌ |
| `DELETE /api/announcements/{id}` | ✅ | ❌ | ❌ | ❌ |
| **人员模块** | | | | |
| `GET /api/persons` | ✅ | ✅ | ✅ | ❌ |
| `POST /api/persons` | ✅ | ✅ | ❌ | ❌ |
| `PUT /api/persons/{id}` | ✅ | ✅ | ❌ | ❌ |
| `DELETE /api/persons/{id}` | ✅ | ❌ | ❌ | ❌ |
| `GET /api/persons/export` | ✅ | ✅ | ❌ | ❌ |
| **系统管理** | | | | |
| `GET /api/admin/users` | ✅ | ❌ | ❌ | ❌ |
| `POST /api/admin/users` | ✅ | ❌ | ❌ | ❌ |
| `PUT /api/admin/users/{id}/role` | ✅ | ❌ | ❌ | ❌ |
| `DELETE /api/admin/users/{id}` | ✅ | ❌ | ❌ | ❌ |
| `GET /api/admin/audit-logs` | ✅ | ❌ | ❌ | ❌ |

### 3.3 敏感操作权限要求

| 操作 | 必须满足的条件 |
|------|---------------|
| 删除任何资源 | `admin` 或 `property_manager` 且操作者与资源同管辖范围 |
| 批量操作（batch-*） | `admin` 或 `property_manager` |
| 数据导出（export） | `admin` 或 `property_manager` |
| 修改其他用户角色 | `admin` |
| 修改自己密码 | 需验证旧密码 |
| 删除自己 | 禁止 |

### 3.4 APIGateway 授权策略实现

#### 方式一：YARP Authorization Policy（推荐）

在 `Program.cs` 中定义策略：

```csharp
// 定义角色策略
builder.Services.AddAuthorization(options =>
{
    // 默认策略：任何已认证用户
    options.DefaultPolicy = new AuthorizationPolicy(
        new[] { new Requirement(ClaimType = "role", Value = "admin") },
        Array.Empty<string>()
    );
    
    // 工单只读策略
    options.AddPolicy("Tickets.Read", policy =>
        policy.RequireAssertion(context =>
        {
            var role = context.User.FindFirst(ClaimTypes.Role)?.Value;
            return role is "admin" or "property_manager" or "operator" or "resident";
        }));

    // 工单写策略（创建/处理）
    options.AddPolicy("Tickets.Write", policy =>
        policy.RequireAssertion(context =>
        {
            var role = context.User.FindFirst(ClaimTypes.Role)?.Value;
            return role is "admin" or "property_manager" or "operator";
        }));

    // 工单删除策略
    options.AddPolicy("Tickets.Delete", policy =>
        policy.RequireAssertion(context =>
        {
            var role = context.User.FindFirst(ClaimTypes.Role)?.Value;
            return role is "admin" or "property_manager";
        }));

    // 数据导出策略
    options.AddPolicy("DataExport", policy =>
        policy.RequireAssertion(context =>
        {
            var role = context.User.FindFirst(ClaimTypes.Role)?.Value;
            return role is "admin" or "property_manager";
        }));

    // Admin 全局策略
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("admin"));
});
```

#### 方式二：路由级强制授权（appsettings.json）

```json
{
  "ReverseProxy": {
    "Routes": {
      "tickets": {
        "ClusterId": "ticket-service",
        "Match": { "Path": "/api/tickets/{*remaining}" },
        "Authorization": "Tickets.Write",
        "AuthorizationPolicy": {
          "RequiredRoles": ["admin", "property_manager", "operator", "resident"],
          "SensiteveActions": {
            "DELETE": ["admin", "property_manager"],
            "POST:/batch": ["admin", "property_manager"]
          }
        }
      }
    }
  }
}
```

### 3.5 微服务内二次校验

> **重要**：APIGateway 的授权不能替代微服务内的授权。每个微服务在处理请求时必须重新验证权限，防止直接调用微服务绕过网关。

```csharp
// 在每个微服务的 Controller/Handler 中
public class TicketController
{
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // 二次校验：只有 admin 和 property_manager 可以删除
        if (userRole is not ("admin" or "property_manager"))
            return Forbid();

        // 校验管辖范围（property_manager 只能删自己的数据）
        if (userRole == "property_manager")
        {
            var ticket = await _ticketService.GetByIdAsync(id);
            if (ticket.OwnerManagerId != int.Parse(userId))
                return Forbid("无权限操作此工单");
        }

        await _ticketService.DeleteAsync(id);
        return Ok();
    }
}
```

---

## 四、审计日志设计

### 4.1 审计日志表结构（MySQL）

```sql
CREATE TABLE audit_logs (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    
    -- 时间戳
    occurred_at TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    
    -- 操作者信息
    user_id INT NULL COMMENT '用户ID（NULL表示系统/匿名）',
    username VARCHAR(50) NOT NULL,
    user_role VARCHAR(20) NOT NULL,
    
    -- 操作信息
    action VARCHAR(100) NOT NULL COMMENT '操作类型，如 LOGIN, LOGOUT, CREATE_TICKET, DELETE_DEVICE',
    resource_type VARCHAR(50) NOT NULL COMMENT '资源类型，如 Ticket, Device, User',
    resource_id VARCHAR(64) NULL COMMENT '资源ID（如GUID）',
    description TEXT NULL COMMENT '操作描述，JSON格式存储额外信息',
    
    -- 请求上下文
    ip_address VARCHAR(45) NOT NULL COMMENT 'IPv4或IPv6',
    user_agent VARCHAR(512) NULL,
    request_method VARCHAR(10) NOT NULL,
    request_path VARCHAR(256) NOT NULL,
    request_body TEXT NULL COMMENT '请求体（脱敏后），仅记录敏感操作',
    
    -- 结果
    result ENUM('SUCCESS', 'FAILURE', 'PARTIAL') NOT NULL DEFAULT 'SUCCESS',
    error_code VARCHAR(32) NULL,
    error_message TEXT NULL,
    
    -- 关联追踪
    trace_id VARCHAR(64) NULL COMMENT '分布式追踪ID',
    
    INDEX idx_occurred_at (occurred_at),
    INDEX idx_user_id (user_id),
    INDEX idx_action (action),
    INDEX idx_resource (resource_type, resource_id),
    INDEX idx_ip (ip_address)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

### 4.2 审计日志记录范围

| 类别 | 具体操作 | 记录内容 |
|------|----------|----------|
| **认证类** | LOGIN, LOGOUT, REFRESH_TOKEN, CHANGE_PASSWORD | 用户、IP、结果、失败原因 |
| **创建类** | CREATE_* | 用户、创建的数据摘要 |
| **修改类** | UPDATE_* | 用户、修改前后的差异（仅记录变更字段） |
| **删除类** | DELETE_* | 用户、被删除数据的标识 |
| **敏感操作** | EXPORT_DATA, BATCH_OPERATION, CHANGE_ROLE | 用户、操作参数、影响范围 |
| **失败操作** | 所有操作（仅当失败时） | 错误码、错误消息 |

### 4.3 日志记录代码示例

#### 中间件方式（推荐）

在 APIGateway 层统一记录，不需要每个微服务单独实现：

```csharp
// AuditLoggingMiddleware.cs
public class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLoggingMiddleware> _logger;

    // 定义需要审计的路径模式
    private static readonly string[] AuditablePaths = { "/api/auth", "/api/tickets", "/api/devices", "/api/persons" };
    private static readonly string[] SensitiveMethods = { "POST", "PUT", "DELETE" };

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        
        // 判断是否需要审计
        var shouldAudit = ShouldAuditRequest(path, context.Request.Method);
        
        if (!shouldAudit)
        {
            await _next(context);
            return;
        }

        var traceId = context.TraceIdentifier;
        var startTime = DateTime.UtcNow;
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = context.Request.Headers.UserAgent.ToString();

        // 提取用户信息（如果已认证）
        int? userId = null;
        string? username = null;
        string? userRole = null;

        if (context.User.Identity?.IsAuthenticated == true)
        {
            userId = int.TryParse(context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : null;
            username = context.User.FindFirst(ClaimTypes.Name)?.Value;
            userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;
        }

        // 读取请求体（用于审计）
        string? requestBody = null;
        if (context.Request.ContentLength > 0 && context.Request.ContentLength < 4096)
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
        }

        // 执行请求
        await _next(context);

        // 记录审计日志
        var duration = DateTime.UtcNow - startTime;
        var result = context.Response.StatusCode < 400 ? "SUCCESS" : "FAILURE";
        
        await SaveAuditLogAsync(new AuditLogEntry
        {
            TraceId = traceId,
            UserId = userId,
            Username = username ?? "anonymous",
            UserRole = userRole ?? "unknown",
            Action = MapToAction(context.Request.Method, path),
            ResourceType = ExtractResourceType(path),
            ResourceId = ExtractResourceId(path),
            Description = BuildDescription(context.Request.Method, path, requestBody),
            IpAddress = ipAddress,
            UserAgent = userAgent,
            RequestMethod = context.Request.Method,
            RequestPath = path,
            RequestBody = SanitizeForAudit(requestBody),
            Result = result,
            ErrorCode = context.Response.StatusCode.ToString()
        });
    }

    private static bool ShouldAuditRequest(string path, string method)
    {
        if (!AuditablePaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            return false;
        if (method == "GET") return false; // GET 通常不记录详细审计
        return true;
    }

    private static string MapToAction(string method, string path)
    {
        var action = method.ToUpper();
        if (path.Contains("/tickets")) action += "_TICKET";
        else if (path.Contains("/devices")) action += "_DEVICE";
        else if (path.Contains("/persons")) action += "_PERSON";
        else if (path.Contains("/auth/login")) action = "LOGIN";
        else if (path.Contains("/auth/logout")) action = "LOGOUT";
        else if (path.Contains("/auth/refresh")) action = "REFRESH_TOKEN";
        return action;
    }

    private static string? SanitizeForAudit(string? body)
    {
        if (string.IsNullOrEmpty(body)) return null;
        // 移除密码等敏感字段
        try
        {
            var json = JsonDocument.Parse(body);
            // 移除 password, passwordHash, token, refreshToken 等字段
            return json.RootElement.GetRawText();
        }
        catch { return body; }
    }
}
```

#### 登录/登出审计（AuthService 增强）

```csharp
// 登录时
app.MapPost("/api/auth/login", async (LoginRequest request, MySqlConnection db, HttpContext ctx) =>
{
    // ... 验证逻辑 ...

    // 审计日志
    await WriteAuditLogAsync(db, new AuditLogEntry
    {
        UserId = user.Id,
        Username = user.Username,
        UserRole = user.Role,
        Action = "LOGIN",
        Result = "SUCCESS",
        IpAddress = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        UserAgent = ctx.Request.Headers.UserAgent.ToString(),
        RequestMethod = "POST",
        RequestPath = "/api/auth/login"
    });
});

// 登出时
app.MapPost("/api/auth/logout", [Authorize] async (HttpContext context, MySqlConnection db) =>
{
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var jti = context.User.FindFirst("jti")?.Value;

    // 加入黑名单
    if (!string.IsNullOrEmpty(jti))
    {
        // 将 jti 加入黑名单，expires_at = token原始过期时间
    }

    await WriteAuditLogAsync(db, new AuditLogEntry
    {
        UserId = int.Parse(userId),
        Username = context.User.FindFirst(ClaimTypes.Name)?.Value,
        UserRole = context.User.FindFirst(ClaimTypes.Role)?.Value,
        Action = "LOGOUT",
        Result = "SUCCESS",
        IpAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        UserAgent = context.Request.Headers.UserAgent.ToString()
    });
});
```

### 4.4 日志存储方案对比

| 方案 | 适用场景 | 优点 | 缺点 |
|------|----------|------|------|
| **MySQL 审计表** (本文档采用) | 数据量 < 1000万/天 | 实现简单，与业务DB同库查询方便 | 大数据量时需分区表和索引维护 |
| **ELK (Elasticsearch)** | 数据量 > 1000万/天 | 全文检索、聚合分析强大 | 部署复杂，需额外运维 |
| **MySQL + ELK 混合** | 中大型系统 | 热数据在MySQL，冷数据到ELK | 架构复杂 |

> **建议**：当前 WO-Property 规模使用 MySQL 审计表即可，每日自动归档或分表（如 `audit_logs_2026_05`）。

### 4.5 日志查询接口（Admin 专用）

```csharp
// GET /api/admin/audit-logs
app.MapGet("/api/admin/audit-logs", [Authorize(Roles = "admin")] async (HttpContext ctx, MySqlConnection db) =>
{
    var userId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    
    var page = int.TryParse(ctx.Request.Query["page"], out var p) ? p : 1;
    var pageSize = Math.Min(int.TryParse(ctx.Request.Query["pageSize"], out var ps) ? ps : 50, 200);
    var startDate = ctx.Request.Query["startDate"].FirstOrDefault();
    var endDate = ctx.Request.Query["endDate"].FirstOrDefault();
    var action = ctx.Request.Query["action"].FirstOrDefault();
    var username = ctx.Request.Query["username"].FirstOrDefault();

    var sql = @"SELECT * FROM audit_logs WHERE 1=1";
    var parameters = new List<MySqlParameter>();

    if (!string.IsNullOrEmpty(startDate))
    {
        sql += " AND occurred_at >= @StartDate";
        parameters.Add(new MySqlParameter("@StartDate", DateTime.Parse(startDate)));
    }
    if (!string.IsNullOrEmpty(endDate))
    {
        sql += " AND occurred_at <= @EndDate";
        parameters.Add(new MySqlParameter("@EndDate", DateTime.Parse(endDate)));
    }
    if (!string.IsNullOrEmpty(action))
    {
        sql += " AND action LIKE @Action";
        parameters.Add(new MySqlParameter("@Action", $"%{action}%"));
    }
    if (!string.IsNullOrEmpty(username))
    {
        sql += " AND username LIKE @Username";
        parameters.Add(new MySqlParameter("@Username", $"%{username}%"));
    }

    sql += " ORDER BY occurred_at DESC LIMIT @Offset, @Limit";
    parameters.Add(new MySqlParameter("@Offset", (page - 1) * pageSize));
    parameters.Add(new MySqlParameter("@Limit", pageSize));

    // 返回分页结果
    // ...
});
```

---

## 五、完整请求流程图

```
┌──────────────┐     ┌─────────────┐     ┌──────────────┐     ┌─────────────┐
│   客户端      │     │  APIGateway  │     │  AuthService │     │   MySQL     │
│  (Vue/小程序) │     │  (YARP+JWT)  │     │  (5006)      │     │             │
└──────┬───────┘     └──────┬──────┘     └──────┬───────┘     └──────┬──────┘
       │                    │                    │                    │
       │  1.登录请求         │                    │                    │
       │  POST /api/auth/login                    │                    │
       │──────────────────►│                    │                    │
       │                    │                    │                    │
       │                    │  2.验证并颁发token  │                    │
       │                    │───────────────────►│                    │
       │                    │                    │  3.查询用户         │
       │                    │                    │───────────────────►│
       │                    │                    │◄───────────────────│
       │                    │                    │                    │
       │                    │                    │  4.生成JWT + Refresh│
       │                    │                    │  5.存储RefreshToken│
       │                    │                    │───────────────────►│
       │                    │                    │◄───────────────────│
       │                    │                    │                    │
       │                    │  6.返回token      │                    │
       │◄───────────────────│◄───────────────────│                    │
       │                    │                    │                    │
       │  7.业务请求(token)  │                    │                    │
       │  GET /api/tickets  │                    │                    │
       │───────────────────►│                    │                    │
       │                    │                    │                    │
       │                    │  8.验证JWT签名     │                    │
       │                    │  9.检查黑名单      │                    │
       │                    │  (查询MySQL)      │                    │
       │                    │  ─────────────────┤                    │
       │                    │  (如用Redis则跳过) │                    │
       │                    │                    │                    │
       │                    │  10.检查角色权限   │                    │
       │                    │                    │                    │
       │                    │  11.转发至后端服务  │                    │
       │                    │────────────────────────────────────────►
       │                    │                    │                    │
       │                    │                    │   12.微服务二次校验 │
       │                    │                    │───────────────────►│
       │                    │                    │◄───────────────────│
       │                    │                    │                    │
       │  13.返回数据       │                    │                    │
       │◄───────────────────│                    │                    │
       │                    │                    │                    │
       │  14.AccessToken快过期，前端发起刷新    │                    │
       │  POST /api/auth/refresh                 │                    │
       │───────────────────►│───────────────────►│                    │
       │                    │                    │                    │
       │                    │  15.验证RefreshToken                    │
       │                    │  16.查询MySQL黑名单│                    │
       │                    │  ─────────────────┤                    │
       │                    │                    │                    │
       │                    │  17.删除旧RT，创建新RT│                   │
       │                    │  18.生成新AccessToken│                   │
       │                    │                    │                    │
       │  19.返回新token    │                    │                    │
       │◄───────────────────│                    │                    │
```

---

## 六、实施优先级

### Phase 1：核心安全（立即实施）

| 任务 | 工作量 | 优先级 |
|------|--------|--------|
| 修改 Access Token 有效期为 15 分钟 | 低 | P0 |
| 实现 token 黑名单表和查询 | 中 | P0 |
| 登出时加入黑名单 + 删除 RefreshToken | 低 | P0 |
| 密码修改后拉黑所有旧会话 | 低 | P0 |
| 审计日志表和基础中间件 | 中 | P0 |

### Phase 2：权限矩阵（1-2周）

| 任务 | 工作量 | 优先级 |
|------|--------|--------|
| APIGateway 添加 AuthorizationPolicy | 中 | P1 |
| 微服务内二次校验 | 高 | P1 |
| 敏感操作（删除/导出/批量）强制校验 | 中 | P1 |
| 审计日志记录登录/登出/改密 | 低 | P1 |

### Phase 3：生产加固（2-4周）

| 任务 | 工作量 | 优先级 |
|------|--------|--------|
| Redis 黑名单缓存（高性能） | 中 | P2 |
| ELK 日志收集（可选） | 高 | P2 |
| 全链路追踪（TraceId） | 中 | P2 |
| 防暴力破解（登录失败计数） | 中 | P2 |

---

## 七、配置参考

### 7.1 AuthService 环境变量

```bash
# .env 或 docker-compose.yml
JWT_SECRET_KEY=your-256-bit-secret-key-here-change-in-production
JWT_ACCESS_TOKEN_EXPIRY_MINUTES=15
JWT_REFRESH_TOKEN_EXPIRY_DAYS=7
DB_CONNECTION_STRING=Server=localhost;Port=3306;Database=wo_property;User=woproperty;Password=WOProperty2026!;
```

### 7.2 APIGateway appsettings.json 补充

```json
{
  "JwtSettings": {
    "SecretKey": "WO-Property-Management-Unified-Secret-Key-2026-For-All-Services",
    "Issuer": "wo-property-unified-auth",
    "Audience": "wo-property-services",
    "AccessTokenExpiryMinutes": 15,
    "ClockSkewSeconds": 0
  },
  "BlacklistCheck": {
    "Enabled": true,
    "CacheExpirySeconds": 60,
    "UseRedis": false
  },
  "RateLimit": {
    "Global": {
      "PermitLimit": 1000,
      "WindowSeconds": 1
    },
    "PerIp": {
      "PermitLimit": 100,
      "WindowSeconds": 1
    }
  }
}
```

---

## 八、测试场景

| # | 场景 | 预期结果 |
|---|------|----------|
| T1 | 登录后 Access Token 15分钟内有效 | ✅ |
| T2 | Access Token 过期后使用 RefreshToken 刷新 | ✅ 获取新token |
| T3 | Access Token 过期后未带 RefreshToken | ❌ 401 |
| T4 | 登出后旧 Access Token 请求 | ❌ 401（黑名单命中） |
| T5 | 修改密码后旧 RefreshToken 刷新 | ❌ 失败，用户被全量黑名单 |
| T6 | admin 可删除工单 | ✅ |
| T7 | operator 尝试删除工单 | ❌ 403 |
| T8 | resident 访问 GET /api/devices | ✅ |
| T9 | resident 尝试导出设备数据 | ❌ 403 |
| T10 | 登录失败记录审计日志 | ✅ |
| T11 | 批量删除操作记录审计日志 | ✅ |

---

_文档结束_