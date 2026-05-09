# WO-Property 健康检查端点规范

## 概述

所有微服务统一使用 `/health` 端点进行健康检查，支持：
- MySQL 连接检测（3秒超时）
- 依赖服务状态报告
- 统一 JSON 响应格式

## 响应格式

### 健康状态 (200 OK)
```json
{
  "status": "healthy",
  "service": "WO.Property.XXXService",
  "version": "1.0.0",
  "timestamp": "2026-05-09T14:30:00Z",
  "dependencies": {
    "mysql": { "status": "healthy" },
    "PersonService": { "status": "healthy" }
  }
}
```

### 不健康状态 (503 Service Unavailable)
```json
{
  "status": "unhealthy",
  "service": "WO.Property.XXXService",
  "version": "1.0.0",
  "timestamp": "2026-05-09T14:30:00Z",
  "dependencies": {
    "mysql": { "status": "unhealthy" }
  }
}
```

## 服务端点

| 服务 | 端口 | 路径 |
|------|------|------|
| APIGateway | 5000 | `GET /health` |
| MasterDataService | 5019 | `GET /health` |
| PersonService | 5018 | `GET /health` |
| TicketService | 5002 | `GET /api/health` |
| AuthService | 5006 | `GET /health` |

## 检测内容

### APIGateway
- 基础服务状态（无依赖检查）

### MasterDataService
- MySQL 连接（SELECT 1）

### PersonService
- MySQL 连接（EF Core CanConnectAsync）

### TicketService
- MySQL 连接（EF Core CanConnectAsync）

### AuthService
- MySQL 连接（SELECT 1）

## 测试命令

```bash
# 测试所有服务健康检查
curl -s http://localhost:5000/health | jq
curl -s http://localhost:5019/health | jq
curl -s http://localhost:5018/health | jq
curl -s http://localhost:5002/api/health | jq
curl -s http://localhost:5006/health | jq

# 验证 503（MySQL 挂掉时）
# 停止 MySQL 后访问任意服务的 /health 应返回 503
```

## 超时配置

- MySQL 连接超时：3秒
- 所有依赖检查超时：3秒

## 实现要点

```csharp
// 统一健康检查逻辑示例
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
        service = "WO.Property.XXXService",
        version = "1.0.0",
        timestamp = DateTime.UtcNow,
        dependencies = new Dictionary<string, object>
        {
            ["mysql"] = new { status = mysqlHealthy ? "healthy" : "unhealthy" }
        }
    };
    
    return Results.Json(response, statusCode: httpStatus);
}).AllowAnonymous();
```

## 修改文件清单

- `src/WO.Property.APIGateway/Program.cs`
- `src/WO.Property.MasterDataService/Program.cs`
- `src/WO.Property.PersonService/Program.cs`
- `src/WO.Property.TicketService/Program.cs`
- `src/WO.Property.AuthService/Program.cs`