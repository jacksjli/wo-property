# WO-Property 生产环境日志体系

> 文档版本: v1.0.0 | 更新日期: 2026-05-10 | 状态: 已实现

---

## 1. 架构概览

```
┌─────────────────────────────────────────────────────────────────┐
│                      .NET 微服务群                                │
│  APIGateway │ PersonService │ TicketService │ DispatchService  │
│  MaterialService │ AuthService │ MasterDataService ...          │
└──────────────┬──────────────────────────────────────────────────┘
               │ JSON Log Files (logs/*.log)
               ▼
┌──────────────────────────────────────────────────────────────────┐
│                        Filebeat                                  │
│           (每服务一个实例，收集日志文件)                            │
└──────────────────────────┬───────────────────────────────────────┘
                           │ Beats Protocol (5044)
                           ▼
┌──────────────────────────────────────────────────────────────────┐
│                       Logstash                                    │
│   - JSON 解析                                                      │
│   - 字段提取/重命名                                                │
│   - 按服务名路由索引                                                │
│   - traceId / elapsed_ms 提取                                      │
└──────────────────────────┬───────────────────────────────────────┘
                           │ HTTP (9200)
                           ▼
┌──────────────────────────────────────────────────────────────────┐
│                    Elasticsearch                                  │
│   - 按服务+日期建索引: apigateway-2026.05.10                      │
│   - ILM 策略: 7天热存 → 30天删除                                    │
│   - 索引模板统一 mapping                                            │
└──────────────────────────┬───────────────────────────────────────┘
                           │ HTTP (5601)
                           ▼
┌──────────────────────────────────────────────────────────────────┐
│                         Kibana                                    │
│   - 日志搜索                                                       │
│   - 请求耗时分析                                                   │
│   - 错误率追踪                                                     │
│   - 告警规则配置                                                   │
└──────────────────────────────────────────────────────────────────┘
```

---

## 2. 日志格式规范

所有 .NET 服务输出统一 JSON 格式日志:

```json
{
  "@timestamp": "2026-05-10T12:00:00.000Z",
  "level": "Information",
  "message": "HTTP GET /api/persons responded 200 in 45ms [TraceId:a1b2c3d4e5f6]",
  "ServiceName": "PersonService",
  "Application": "WO-Property",
  "machine_name": "wo-property-server",
  "traceId": "a1b2c3d4e5f6",
  "elapsedMs": 45,
  "http_method": "GET",
  "http_path": "/api/persons",
  "http_status_code": 200
}
```

异常日志额外字段:
```json
{
  "level": "Error",
  "exception": "System.NullReferenceException: Object reference not set...",
  "message": "Unhandled exception [TraceId:xyz] /api/persons",
  "context": {}
}
```

### 标准日志字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `@timestamp` | ISO8601 | 日志时间 |
| `level` | keyword | Information/Warning/Error |
| `message` | text | 日志消息 |
| `ServiceName` | keyword | 服务名 |
| `traceId` | keyword | 请求追踪ID |
| `exception` | text | 异常堆栈 |
| `elapsedMs` | float | 请求耗时(毫秒) |
| `http_method` | keyword | HTTP方法 |
| `http_path` | keyword | 请求路径 |
| `http_status_code` | integer | 响应状态码 |

---

## 3. Serilog 配置

### 3.1 统一日志中间件

所有服务的 `Program.cs` 已添加:

1. **Serilog 初始化** (每个服务独立 `LoggerConfiguration`)
2. **UseRequestLogging** 中间件 — 请求入参/出参/耗时/状态码
3. **UseGlobalExceptionHandler** 中间件 — 未捕获异常统一处理

### 3.2 日志文件路径

```
项目根目录/
└── logs/
    ├── apigateway-20260510.log
    ├── apigateway-20260511.log
    ├── personservice-20260510.log
    ├── ticketservice-20260510.log
    ├── dispatchservice-20260510.log
    └── materialservice-20260510.log
```

- 滚动策略: 按天滚动 (`RollingInterval.Day`)
- 文件大小限制: 100MB/文件 (超限自动切新文件)
- 保留策略: 最多30天

### 3.3 禁止 Console.WriteLine

所有 `Console.WriteLine` 已替换为 `Log.Information/Warning/Error`。
生产构建时，Serilog Console Sink 输出 JSON 格式，便于 Filebeat 采集。

---

## 4. ELK Stack 配置

### 4.1 服务启动

```bash
# 进入 ELK 配置目录
cd /Users/mac/Projects/WO-Property-Management/docker/elk

# 启动 ELK Stack
docker compose -f docker-compose.elk.yml up -d

# 检查服务状态
docker compose -f docker-compose.elk.yml ps

# 查看 Kibana
# 访问 http://localhost:5601
```

### 4.2 索引命名规范

```
{service_name}-yyyy.MM.dd
例如:
  apigateway-2026.05.10
  personservice-2026.05.10
  ticketservice-2026.05.10
  dispatchservice-2026.05.10
  materialservice-2026.05.10
```

### 4.3 Elasticsearch ILM 策略 (30天滚动删除)

| 阶段 | 保留时间 | 操作 |
|------|---------|------|
| Hot | 1天或1GB | 滚动(Rollover) |
| Warm | 7天 | Shrink + ForceMerge |
| Delete | 30天 | 自动删除 |

### 4.4 Kibana 使用

1. 访问 `http://localhost:5601`
2. 创建 Index Pattern: `apigateway-*` (或 `wo-property-*` 匹配全部)
3. Discovery 页面搜索日志
4. 示例查询:
   - `log_level: ERROR` — 所有错误
   - `service_name: PersonService AND http_path: /api/persons` — 特定API日志
   - `traceId: a1b2c3d4` — 按请求追踪

---

## 5. 前端日志方案 (admin-portal)

### 5.1 ErrorBoundary 捕获 Vue 错误

`src/main.ts` 全局错误处理器已配置:

```typescript
app.config.errorHandler = (err, instance, info) => {
  // 使用 sendBeacon 上报到日志服务
  reportError({ err, info, url: window.location.href })
}

window.addEventListener('unhandledrejection', (event) => {
  reportError({ reason: event.reason, url: window.location.href })
})
```

### 5.2 生产环境屏蔽 console.log

`vite.config.ts` 配置:

```typescript
define: {
  __VUE_PROD_HYDRATION_MISMATCH_DETAILS__: 'false',
},
// 生产环境可添加:
// 'console.log': 'void 0'
```

### 5.3 前端错误上报

```typescript
function reportError(payload: object) {
  if (import.meta.env.PROD) {
    navigator.sendBeacon('/api/frontend-logs', JSON.stringify(payload))
  }
  console.error(payload) // 仅开发环境
}
```

---

## 6. 日志字段清单

### 请求日志 (每API调用一条)

| 字段 | 示例 | 说明 |
|------|------|------|
| traceId | a1b2c3d4e5f6g7h8 | 请求唯一ID，从请求头 `X-Trace-Id` 传递 |
| http_method | GET | HTTP方法 |
| http_path | /api/persons | 请求路径 |
| http_status_code | 200 | 响应状态码 |
| elapsedMs | 45 | 耗时(毫秒) |
| service_name | PersonService | 服务名 |

### 应用日志

| 字段 | 示例 | 说明 |
|------|------|------|
| level | Information | 日志级别 |
| message | Database initialized | 日志内容 |
| exception | (异常堆栈) | 仅错误时有值 |
| ServiceName | PersonService | 服务标识 |

---

## 7. Filebeat 服务实例配置

每個 .NET 微服务在 `docker/elk/filebeat/` 有独立配置:

| 文件 | 服务 | 端口 |
|------|------|------|
| `apigateway.yml` | APIGateway | 5000 |
| `personservice.yml` | PersonService | 5018 |
| `ticketservice.yml` | TicketService | 5002 |
| `dispatchservice.yml` | DispatchService | 5003 |
| `materials.yml` | MaterialService | 5004 |
| `authservice.yml` | AuthService | 5006 |

如需新增服务，复制一个 filebeat 配置并修改:
- `paths`: 指向对应日志文件
- `fields.service`: 服务标识
- `output.logstash.hosts`: Logstash 地址

---

## 8. Kibana 仪表板建议

### 8.1 请求量仪表板
- X-axis: @timestamp (按小时)
- Y-axis: Count
- Split series: service_name

### 8.2 错误率仪表板
- Filter: `log_level: ERROR`
- 按 service_name 分组统计

### 8.3 平均响应时间
- Y-axis: Average of elapsedMs
- 按 http_path 分组

---

## 9. 告警规则 (ElastAlert)

### 错误率告警
- 触发条件: 5分钟内 ERROR 日志超过10条
- 告警方式: Debug (生产改为 email/slack)

### 自定义告警
在 `docker/elk/elastalert/rules/` 添加 YAML 配置文件后重启 elastalert 容器。

---

## 10. 生产环境检查清单

- [ ] Serilog NuGet 包已添加到 `WO.Property.Shared.csproj`
- [ ] 所有服务 Program.cs 已添加日志初始化代码
- [ ] `logs/` 目录存在且有写权限
- [ ] Filebeat 配置覆盖所有服务日志文件
- [ ] Elasticsearch ILM 策略已创建
- [ ] Kibana Index Pattern 已创建
- [ ] 前端 ErrorBoundary 已配置
- [ ] 生产环境 `console.log` 已屏蔽

---

## 附录: 日志相关文件清单

```
WO-Property-Management/
├── src/
│   ├── WO.Property.Shared/
│   │   ├── Logging/
│   │   │   └── LoggingExtensions.cs    ← 统一日志中间件
│   │   └── WO.Property.Shared.csproj   ← Serilog 依赖
│   ├── WO.Property.APIGateway/
│   │   └── Program.cs                  ← 已添加 Serilog
│   ├── WO.Property.PersonService/
│   │   └── Program.cs                  ← 已添加 Serilog
│   ├── WO.Property.TicketTypeService/
│   │   └── Program.cs                  ← 已添加 Serilog
│   ├── WO.Property.MaterialService/
│   │   └── Program.cs                  ← 已添加 Serilog
│   ├── WO.Property.DispatchService/
│   │   └── Program.cs                  ← 已添加 Serilog
│   └── admin-portal/
│       ├── main.ts                     ← ErrorBoundary
│       └── vite.config.ts              ← 生产环境 console 屏蔽
├── docker/
│   ├── elk/
│   │   ├── docker-compose.elk.yml      ← ELK Stack
│   │   ├── filebeat/                   ← 每服务一个配置
│   │   │   ├── apigateway.yml
│   │   │   ├── personservice.yml
│   │   │   ├── ticketservice.yml
│   │   │   └── dispatchservice.yml
│   │   ├── logstash/
│   │   │   ├── config/logstash.yml
│   │   │   └── pipeline/woproperty.conf
│   │   ├── elasticsearch/ilm/
│   │   │   ├── wo-property-logs-policy.json
│   │   │   └── wo-property-template.json
│   │   └── elastalert/
│   │       ├── config.yaml
│   │       └── rules/error-alert.yaml
│   └── elk/docker-compose.elk.yml      ← ELK 启动入口
└── docs/
    └── PRODUCTION_LOGGING.md           ← 本文档
```