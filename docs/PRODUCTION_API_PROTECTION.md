# WO-Property API 防护机制生产指南

> 版本: 1.1.0 | 更新日期: 2026-05-10 | 状态: ✅ 已实现

---

## 目录

1. [架构概览](#1-架构概览)
2. [Gateway 层限流](#2-gateway-层限流)
3. [防护机制](#3-防护机制)
4. [登录防护](#4-登录防护)
5. [CORS 配置](#5-cors-配置)
6. [配置参考](#6-配置参考)
7. [API 响应格式](#7-api-响应格式)
8. [运维建议](#8-运维建议)

---

## 1. 架构概览

```
[客户端]
    ↓ HTTPS
[API Gateway (Ocelot :5000)]
    ├── RateLimiter（限流）
    ├── CORS（跨域）
    ├── RequestSecurity（请求安全）
    │   ├── Body Size Limit
    │   ├── SQL Injection 检测
    │   ├── XSS 检测
    │   ├── HTTP Method 控制
    │   ├── Header 数量限制
    │   └── Slowloris 防护
    ├── Authentication
    ├── Authorization
    └── ReverseProxy (YARP)
           ↓
    ┌──────┴──────┐
    ↓              ↓
[AuthService]   [其他微服务]
 (:5006)         (5002/5018/5019...)
```

### 限流分层

| 层级 | 限流对象 | 阈值 | 窗口 |
|------|----------|------|------|
| 全局 | 所有请求 | 1000 req/s | 1秒 |
| IP级 | 每个IP | 100 req/min | 1分钟 |
| 用户级 | 每个用户 | 200 req/min | 1分钟 |
| 敏感接口 | /api/auth/login | 10 req/min | 1分钟 |

---

## 2. Gateway 层限流

### 2.1 实现机制

使用 .NET 内置 `System.Threading.RateLimiting` + `Microsoft.AspNetCore.RateLimiting`。

**中间件顺序：**

```csharp
1. app.UseRateLimiter()          // 全局限流
2. app.UseCors()                  // CORS
3. 安全中间件（Body/SQL/XSS/Header）
4. app.UseAuthentication()
5. app.UseAuthorization()
6. app.MapReverseProxy()          // YARP
```

### 2.2 限流响应

**触发条件：** 请求频率超过配置的阈值

**HTTP 响应：**
```
HTTP/1.1 429 Too Many Requests
Content-Type: application/json
Retry-After: 60
X-RateLimit-Remaining: 0
X-RateLimit-Reset: 1623123456
```

**响应体：**
```json
{
  "error": "TooManyRequests",
  "code": "RATE_LIMIT_EXCEEDED",
  "message": "请求过于频繁，请稍后再试",
  "retryAfter": 60
}
```

### 2.3 分级限流策略

**普通接口（100次/分钟）：**
- `/api/tickets/*`
- `/api/persons/*`
- `/api/roles/*`
- `/api/master-data/*`

**敏感接口（10次/分钟）：**
- `/api/auth/login`
- `/api/auth/register`
- `/api/payments/*`

---

## 3. 防护机制

### 3.1 请求体大小限制

**配置：** `Security.RequestBodyLimit`

| 配置项 | 值 | 说明 |
|--------|-----|------|
| MaxSizeInBytes | 10MB | 最大请求体 |
| MaxSizeInMB | 10 | 人类可读单位 |
| Enabled | true | 启用开关 |

**触发场景：**
- 文件上传超过 10MB
- POST JSON 数据超过 10MB

**响应：**
```json
{
  "error": "PayloadTooLarge",
  "message": "请求体过大，最大允许 10MB"
}
```

### 3.2 SQL 注入防护

**检测模式：**
```
--, ;--, /*, */, @@, char, nchar, varchar, nvarchar,
alter, begin, cast, create, cursor, declare, delete, drop,
end, exec, execute, fetch, insert, kill, select, sys,
sysobjects, syscolumns, table, update, xp_, 0x, or 1=1
```

**检查范围：**
- Query String 参数
- POST/PUT Body (JSON)

**响应：**
```json
{
  "error": "BadRequest",
  "code": "INVALID_INPUT",
  "message": "请求参数包含非法字符"
}
```

### 3.3 XSS 防护

**检测模式：**
```
<script, </script, javascript:, onerror=, onload=,
onclick=, onmouseover=, onfocus=, onblur=,
<iframe, <object, <embed, <svg, expression(, eval(,
innerHTML, outerHTML
```

**响应：**
```json
{
  "error": "BadRequest",
  "code": "INVALID_INPUT",
  "message": "请求体包含非法字符"
}
```

### 3.4 HTTP 方法控制

| 配置项 | 默认值 | 说明 |
|--------|--------|------|
| AllowHead | false | 禁用 HEAD |
| AllowOptions | true | 允许 OPTIONS |
| BlockUnknownMethods | true | 拒绝未知方法 |

**触发场景：**
- 收到 `HEAD` 请求 → 返回 405
- 收到 `TRACE`/`CONNECT` → 返回 405

### 3.5 Header 限制

| 配置项 | 值 | 说明 |
|--------|-----|------|
| MaxRequestHeadersCount | 64 | 单请求最大头数 |
| MaxHeaderSizeInBytes | 8KB | 单个头最大长度 |
| KeepAliveTimeoutSeconds | 30 | 连接保持超时 |

### 3.6 Slowloris 防护

**配置：** `Security.SlowlorisProtection`

| 配置项 | 值 | 说明 |
|--------|-----|------|
| Enabled | true | 启用 |
| ReadTimeoutSeconds | 15 | 读超时 |
| WriteTimeoutSeconds | 15 | 写超时 |

**原理：** 通过 Kestrel 超时设置防范 Slowloris 攻击。

---

## 4. 登录防护

### 4.1 账户锁定机制

**配置：** `LoginProtection`

| 配置项 | 值 | 说明 |
|--------|-----|------|
| MaxFailuresBeforeLockout | 5 | 锁定前的失败次数 |
| LockoutMinutes | 15 | 锁定持续时间 |
| MaxFailuresBeforeBan | 20 | 封禁IP前的失败次数 |
| BanDurationSeconds | 1800 | IP封禁持续时间（30分钟）|

**流程图：**
```
用户登录失败
    ↓
失败次数 +1
    ↓
失败次数 >= 5 ?
    ├── 否 → 返回 401，继续尝试
    └── 是 → 锁定账户 15 分钟
              ↓
         返回 423 Locked
         {"locked": true, "lockedUntil": "..."}
```

### 4.2 IP 封禁机制

**触发条件：** 同一 IP 在 30 分钟内失败超过 20 次

**响应：**
```json
{
  "success": false,
  "error": "IpBanned",
  "code": "IP_TEMPORARILY_BANNED",
  "message": "IP 已被临时封禁，请在 30 分钟后重试",
  "bannedUntil": "2026-05-10T06:00:00Z"
}
```

### 4.3 验证码接口（预留）

**获取验证码：**
```
GET /api/auth/captcha
```

**响应：**
```json
{
  "captchaId": "a1b2c3d4e5f6",
  "captchaType": "slider",
  "expiresAt": "2026-05-10T05:05:00Z"
}
```

**验证验证码：**
```
POST /api/auth/verify-captcha
{
  "captchaId": "a1b2c3d4e5f6",
  "captchaToken": "xxx"
}
```

### 4.4 登录状态查询

```
GET /api/auth/login-status/{username}
```

**响应（已锁定）：**
```json
{
  "locked": true,
  "lockedUntil": "2026-05-10T05:15:00Z",
  "remainingSeconds": 300
}
```

**响应（正常）：**
```json
{
  "locked": false
}
```

---

## 5. CORS 配置

### 5.1 默认配置

```json
{
  "CORS": {
    "AllowedOrigins": [
      "http://localhost:5173",
      "http://localhost:5174"
    ],
    "AllowedMethods": ["GET", "POST", "PUT", "DELETE", "PATCH"],
    "AllowedHeaders": [
      "Authorization",
      "Content-Type",
      "X-Requested-With",
      "Accept",
      "Origin"
    ],
    "ExposedHeaders": [
      "X-Request-ID",
      "X-RateLimit-Remaining",
      "X-RateLimit-Reset"
    ],
    "AllowCredentials": true,
    "PreflightMaxAgeSeconds": 3600,
    "Enabled": true
  }
}
```

### 5.2 请求示例

**预检请求（Preflight）：**
```
OPTIONS /api/auth/login HTTP/1.1
Origin: http://localhost:5173
Access-Control-Request-Method: POST
Access-Control-Request-Headers: Content-Type, Authorization
```

**预检响应：**
```
HTTP/1.1 204 No Content
Access-Control-Allow-Origin: http://localhost:5173
Access-Control-Allow-Methods: GET, POST, PUT, DELETE, PATCH
Access-Control-Allow-Headers: Content-Type, Authorization
Access-Control-Allow-Credentials: true
Access-Control-Max-Age: 3600
```

### 5.3 前端配置

```javascript
// axios 或 fetch
fetch('/api/auth/login', {
  method: 'POST',
  credentials: 'include',  // 必须设置 withCredentials
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({ username, password })
})
```

---

## 6. 配置参考

### 6.1 APIGateway appsettings.json（完整示例）

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "AllowedOrigins": [
    "http://localhost:5173",
    "http://localhost:5174",
    "http://localhost:5175"
  ],

  "RateLimiting": {
    "Enabled": true,
    "GlobalLimit": 1000,
    "PerIpLimit": 100,
    "PerUserLimit": 200,
    "WindowSeconds": 60,
    "SensitiveEndpoints": [
      "/api/auth/login",
      "/api/auth/register",
      "/api/payments"
    ],
    "SensitiveLimit": 10,
    "Response429": {
      "Enabled": true,
      "RetryAfterHeader": true,
      "Message": "请求过于频繁，请稍后再试"
    }
  },

  "Security": {
    "RequestBodyLimit": {
      "MaxSizeInBytes": 10485760,
      "MaxSizeInMB": 10,
      "Enabled": true
    },
    "SqlInjectionProtection": {
      "Enabled": true,
      "BlockPatterns": true,
      "SanitizeInput": true
    },
    "XssProtection": {
      "Enabled": true,
      "SanitizeHtml": true
    },
    "HttpMethodControl": {
      "AllowHead": false,
      "AllowOptions": true,
      "BlockUnknownMethods": true
    },
    "HeaderLimits": {
      "MaxRequestHeadersCount": 64,
      "MaxHeaderSizeInBytes": 8192,
      "MaxContentLengthInBytes": 104857600,
      "KeepAliveTimeoutSeconds": 30
    },
    "SlowlorisProtection": {
      "Enabled": true,
      "ReadTimeoutSeconds": 15,
      "WriteTimeoutSeconds": 15
    }
  },

  "CORS": {
    "AllowedOrigins": [
      "http://localhost:5173",
      "http://localhost:5174",
      "http://localhost:5175"
    ],
    "AllowedMethods": ["GET", "POST", "PUT", "DELETE", "PATCH"],
    "AllowedHeaders": [
      "Authorization",
      "Content-Type",
      "X-Requested-With",
      "Accept",
      "Origin",
      "X-CSRF-Token",
      "X-Request-ID"
    ],
    "ExposedHeaders": [
      "X-Request-ID",
      "X-RateLimit-Remaining",
      "X-RateLimit-Reset"
    ],
    "AllowCredentials": true,
    "PreflightMaxAgeSeconds": 3600,
    "Enabled": true
  },

  "ReverseProxy": {
    "Routes": { ... },
    "Clusters": { ... }
  }
}
```

### 6.2 AuthService appsettings.json（完整示例）

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",

  "LoginProtection": {
    "Enabled": true,
    "MaxFailuresBeforeLockout": 5,
    "LockoutMinutes": 15,
    "MaxFailuresBeforeBan": 20,
    "BanDurationSeconds": 1800
  },

  "RateLimiting": {
    "Enabled": true,
    "GlobalLimit": 1000,
    "PerIpLimit": 100,
    "PerUserLimit": 200,
    "WindowSeconds": 60,
    "SensitiveEndpoints": ["/api/auth/login"],
    "SensitiveLimit": 10
  },

  "Security": {
    "RequestBodyLimit": {
      "MaxSizeInBytes": 1048576,
      "MaxSizeInMB": 1,
      "Enabled": true
    }
  },

  "CORS": {
    "AllowedOrigins": [
      "http://localhost:5173",
      "http://localhost:5174"
    ],
    "AllowedMethods": ["GET", "POST"],
    "AllowedHeaders": ["Authorization", "Content-Type"],
    "ExposedHeaders": ["X-Request-ID"],
    "AllowCredentials": true,
    "PreflightMaxAgeSeconds": 3600,
    "Enabled": true
  }
}
```

---

## 7. API 响应格式

### 7.1 错误响应标准格式

```json
{
  "error": "ErrorCode",
  "code": "ERROR_CODE",
  "message": "用户友好的错误消息",
  "details": { }
}
```

### 7.2 常见错误码

| HTTP Status | error | code | 说明 |
|-------------|-------|------|------|
| 400 | BadRequest | INVALID_INPUT | 参数非法 |
| 400 | BadRequest | SQL_INJECTION_DETECTED | SQL注入检测 |
| 400 | BadRequest | XSS_DETECTED | XSS攻击检测 |
| 401 | Unauthorized | TOKEN_INVALID | Token无效 |
| 401 | Unauthorized | TOKEN_EXPIRED | Token过期 |
| 403 | Forbidden | ACCESS_DENIED | 无权限 |
| 405 | MethodNotAllowed | METHOD_NOT_ALLOWED | HTTP方法不允许 |
| 413 | PayloadTooLarge | REQUEST_BODY_TOO_LARGE | 请求体过大 |
| 423 | Locked | ACCOUNT_LOCKED | 账户被锁定 |
| 429 | TooManyRequests | RATE_LIMIT_EXCEEDED | 请求过于频繁 |
| 429 | TooManyRequests | IP_BANNED | IP被封禁 |
| 431 | TooManyHeaders | HEADERS_TOO_LARGE | 请求头过大 |
| 500 | InternalError | SERVER_ERROR | 服务器内部错误 |

---

## 8. 运维建议

### 8.1 监控指标

建议监控以下指标：

```sql
-- 查询被锁定的账户
SELECT Username, CreatedAt FROM users
WHERE Status = 'Locked';

-- 查询最近登录失败记录（需在数据库中建表）
SELECT * FROM login_failures
WHERE AttemptTime > NOW() - INTERVAL 1 DAY
ORDER BY AttemptTime DESC;
```

### 8.2 阈值调整建议

| 场景 | 建议值 |
|------|--------|
| 开发环境 | 普通接口 500/分钟，敏感接口 30/分钟 |
| 测试环境 | 普通接口 200/分钟，敏感接口 20/分钟 |
| 生产环境 | 普通接口 100/分钟，敏感接口 10/分钟 |

### 8.3 高可用建议

1. **限流状态存储：** 当前使用内存存储（ConcurrentDictionary），生产环境建议使用 Redis 共享状态

2. **配置热更新：** 可通过挂载配置文件或 ConfigMap 实现不停机更新

3. **日志记录：** 建议记录以下事件：
   - 限流触发（warn 级别）
   - SQL注入/XSS 检测（warn 级别）
   - IP 封禁（info 级别）

4. **熔断策略：** 建议在微服务层面增加熔断器（Hystrix/Polly），防止级联故障

### 8.4 安全加固建议

1. **HTTPS 强制：** 生产环境必须使用 HTTPS
2. **WAF：** 建议在 API Gateway 前部署 WAF（如阿里云 WAF、腾讯云 WAF）
3. **DDoS 防护：** 建议使用云厂商的 DDoS 防护服务
4. **日志审计：** 建议开启 API 访问日志，定期审计异常行为

---

## 修改历史

| 日期 | 版本 | 修改内容 |
|------|------|----------|
| 2026-05-10 | 1.0.0 | 初始版本 |
| 2026-05-10 | 1.1.0 | 增加完整限流、登录防护、CORS 配置 |

---

_本文档由架构师子代理自动生成_