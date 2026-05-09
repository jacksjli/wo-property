# WO-Property 生产环境数据库优化指南

> 版本：1.0.0
> 日期：2026-05-10
> 作者：WO-Property 数据库工程师

---

## 目录

1. [连接池配置](#1-连接池配置)
2. [索引优化](#2-索引优化)
3. [慢查询监控](#3-慢查询监控)
4. [熔断与重试机制](#4-熔断与重试机制)
5. [执行检查清单](#5-执行检查清单)

---

## 1. 连接池配置

### 1.1 推荐连接字符串格式

```_connection-string
Server={HOST};Port=3306;Database=wo_property;User=woproperty;Password={PASSWORD};CharSet=utf8mb4;Pooling=true;Min Pool Size=5;Max Pool Size=100;Connection Idle Timeout=300;Connection Timeout=10;Default Command Timeout=30;
```

| 参数 | 推荐值 | 说明 |
|------|--------|------|
| `Pooling` | `true` | 启用连接池（必须） |
| `Min Pool Size` | `5` | 最小连接数，常驻连接减少冷启动开销 |
| `Max Pool Size` | `100` | 最大连接数，根据并发峰值调整 |
| `Connection Idle Timeout` | `300` | 空闲连接保留时间（秒），5分钟 |
| `Connection Timeout` | `10` | 获取连接超时（秒） |
| `Default Command Timeout` | `30` | 单条 SQL 执行超时（秒） |

### 1.2 各服务当前配置

#### TicketService（端口 5002）

**文件：** `src/WO.Property.TicketService/Program.cs`

```csharp
options.UseMySQL("Server=localhost;Port=3306;Database=wo_property;User=woproperty;Password=WOProperty2026!;CharSet=utf8mb4;Pooling=true;Minimum Pool Size=2;Maximum Pool Size=10;Connection Timeout=10;Connection Idle Timeout=30;Default Command Timeout=30;");
```

**HttpClient 超时配置：**

| 服务 | BaseAddress | Timeout |
|------|-------------|---------|
| PersonService | http://localhost:5018 | 10s |
| DispatchService | http://localhost:5003 | 10s |
| MasterDataService | http://localhost:5019 | 10s |

**调优建议：** 当前 `Max Pool Size=10` 偏低，生产环境建议升至 `50`；`Min Pool Size` 从 `2` 升至 `5`。

```csharp
// 建议调整后的配置
"Pooling=true;Minimum Pool Size=5;Maximum Pool Size=50;Connection Timeout=10;Connection Idle Timeout=300;Default Command Timeout=30;"
```

#### MasterDataService（端口 5019）

**文件：** `src/WO.Property.MasterDataService/Program.cs`

多处硬编码连接字符串，已启用连接池：

```
Pooling=true;Minimum Pool Size=2;Maximum Pool Size=10;Connection Timeout=10;Connection Idle Timeout=60;Default Command Timeout=30;
```

**调优建议：** 统一调整为 `Minimum Pool Size=5;Maximum Pool Size=50`。

#### PersonService（端口 5018）

**文件：** `src/WO.Property.PersonService/Program.cs`

```csharp
var connectionString = "Server=localhost;Port=3306;Database=wo_property;...Pooling=true;Minimum Pool Size=2;Maximum Pool Size=20;Connection Timeout=10;Connection Idle Timeout=60;Default Command Timeout=30;";
```

**调优建议：** `Max Pool Size=20` 建议升至 `50`。

### 1.3 负载均衡建议

| 场景 | 建议 |
|------|------|
| **单实例** | 使用 `Min Pool Size=5` 保持连接常驻 |
| **多实例** | 每个实例 `Max Pool Size` = 总连接数 / 实例数，建议不超过 `50` |
| **读写分离** | 连接池只配置主库；读操作通过 ReadOnly 事务路由到从库 |
| **连接数监控** | 执行 `SHOW STATUS LIKE 'Threads_connected%';` 监控连接数 |

---

## 2. 索引优化

### 2.1 索引脚本

**文件：** `scripts/db_optimization/add_indexes.sql`

**共计 19 个索引**，覆盖以下表和场景：

#### 工单相关表（tickets）

| 索引名 | 字段 | 场景 | 预估提升 |
|--------|------|------|---------|
| `idx_tickets_status_createdAt` | `(Status, CreatedAt DESC)` | 按状态筛选 + 时间排序 | 5-10x |
| `idx_tickets_assignee` | `(AssigneePersonId, CreatedAt DESC)` | 处理人查询 | 5-10x |
| `idx_tickets_creator` | `(CreatorPersonId, CreatedAt DESC)` | 创建人查询 | 5-10x |
| `idx_tickets_ticketCode` | `(TicketCode)` | 工单号精确查找 | 唯一查找即命中 |
| `idx_tickets_priority` | `(Priority, CreatedAt DESC)` | 按优先级统计 | 3-5x |
| `idx_tickets_cursor` | `(CreatedAt DESC, Id DESC)` | 游标分页（替代 OFFSET） | 100x+（深度分页） |

#### 派单任务表（dispatch_tasks）

| 索引名 | 字段 | 场景 | 预估提升 |
|--------|------|------|---------|
| `idx_dispatch_ticketId` | `(TicketId)` | 按工单查派单 | 命中索引 |
| `idx_dispatch_assignee_status` | `(AssignedToPersonId, Status, AssignedAt DESC)` | 员工待处理任务列表 | 5-10x |

#### 人员表（persons）

| 索引名 | 字段 | 场景 | 预估提升 |
|--------|------|------|---------|
| `idx_persons_department` | `(Department, Status, Name)` | 部门人员列表 | 10-50x |
| `idx_persons_status_type` | `(Status, PersonType, Name)` | 在职人员下拉框 | 10-50x |
| `idx_persons_name` | `(Name)` | 姓名前缀搜索（`LIKE '张%'`） | 5x |
| `idx_persons_staffId` | `(StaffId)` | 工号查询 | 唯一查找即命中 |
| `idx_persons_phone` | `(Phone)` | 手机号登录/找回 | 唯一查找即命中 |

#### 公共表（departments / roles）

| 索引名 | 字段 | 场景 |
|--------|------|------|
| `idx_departments_name` | `(Name)` | 部门名称唯一性 |
| `idx_roles_name` | `(Name)` | 角色名称唯一性 |

#### 字段定义表（field_definitions）

| 索引名 | 字段 | 场景 |
|--------|------|------|
| `idx_fd_module` | `(Module, Status, SortOrder)` | 按模块查字段 |
| `idx_fd_isShared_source` | `(IsShared, Source, FieldKey)` | 共享字段筛选 |

#### 关联表（ticket_process_records / module_fields）

| 索引名 | 字段 | 场景 |
|--------|------|------|
| `idx_tpr_ticketId_createdAt` | `(TicketId, CreatedAt DESC)` | 工单处理历史（按时间排序） |
| `idx_mf_module` | `(Module, IsActive, SortOrder)` | 字段关联查询 |

### 2.2 执行顺序和注意事项

**执行顺序：**

```bash
# 1. 先在测试环境执行（数据量大的表可能锁表）
docker exec -i wo-property-mysql mysql -uwoproperty -pWOProperty2026! wo_property < add_indexes.sql

# 2. 验证索引创建成功
SHOW INDEX FROM tickets;
SHOW INDEX FROM persons;

# 3. 检查索引使用情况（上线后观察）
SELECT * FROM performance_schema.table_io_waits_summary_by_index_usage
WHERE OBJECT_SCHEMA = 'wo_property' ORDER BY COUNT_READ DESC;
```

**注意事项：**

- MySQL `CREATE INDEX IF NOT EXISTS` 语法需要 MySQL 8.0+，低版本需手动删除 `IF NOT EXISTS`
- `idx_tickets_cursor` 为游标分页索引，用于替代深度 OFFSET 分页（见 2.3）
- 大表创建索引时可能造成短暂锁表，建议在低峰期执行：`ALTER TABLE tickets ALGORITHM=INPLACE, LOCK=NONE;`

### 2.3 游标分页 vs OFFSET 分页

**原有分页（深度 OFFSET 性能差）：**
```sql
-- OFFSET 10000 时，需要扫描 10020 行才能返回 20 条
SELECT * FROM tickets ORDER BY CreatedAt DESC LIMIT 20 OFFSET 10000
```

**游标分页（性能稳定）：**
```sql
-- 通过索引直接定位到起始位置，无需扫描前面的行
SELECT * FROM tickets
WHERE CreatedAt < {lastCreatedAt}
ORDER BY CreatedAt DESC LIMIT 20
```

API 改用游标分页后，使用 `idx_tickets_cursor` 索引，性能提升 **100x+**。

---

## 3. 慢查询监控

### 3.1 慢查询日志配置

**文件：** `scripts/db_optimization/slow_query_log.sql`

```sql
-- 开启慢查询日志
SET GLOBAL slow_query_log = 'ON';
SET GLOBAL slow_query_log_file = '/var/log/mysql/mysql-slow.log';

-- 阈值 200ms（生产环境建议 100-500ms）
SET GLOBAL long_query_time = 0.2;

-- 记录未使用索引的查询
SET GLOBAL log_queries_not_using_indexes = 'ON';

-- 最少扫描 100 行才记录
SET GLOBAL min_examined_row_limit = 100;
```

**持久化配置（需重启 MySQL）：**

```ini
[mysqld]
slow_query_log = 1
slow_query_log_file = /var/log/mysql/mysql-slow.log
long_query_time = 0.2
log_queries_not_using_indexes = 1
min_examined_row_limit = 100
```

### 3.2 慢查询分析方法

**文件：** `scripts/db_optimization/slow_query_analysis.sql`

包含以下分析模板：

| 查询 | 用途 |
|------|------|
| 执行时间最长的 20 条 | 定位最慢查询 |
| 扫描行数最多的查询 | 发现全表扫描 |
| 返回行少但扫描多 | 典型索引缺失 |
| 高频慢查询统计 | 找反复出现的慢查询 |
| 工单/人员专项分析 | 按业务表分类 |
| 深度分页问题 | 识别 OFFSET 滥用 |
| 表碎片率检查 | 发现需要 OPTIMIZE 的表 |

**pt-query-digest 使用（推荐）：**

```bash
# 分析最近 24 小时
pt-query-digest /var/log/mysql/mysql-slow.log --since='24h'

# 输出到文件
pt-query-digest /var/log/mysql/mysql-slow.log --since='168h' > /var/log/mysql/query-report.txt

# 只分析超过 1 秒的查询
pt-query-digest /var/log/mysql/mysql-slow.log --filter='$event->{query_time} >= 1'

# 分析特定表
pt-query-digest /var/log/mysql/mysql-slow.log --filter='$event->{sql_text} =~ /tickets/i'
```

### 3.3 告警阈值设置

| 指标 | 告警阈值 | 处理建议 |
|------|---------|---------|
| 单次查询时间 | > 1s | 检查是否缺失索引或存在全表扫描 |
| 单次查询扫描行数 | > 10,000 | 检查 WHERE 条件是否命中索引 |
| 扫描/返回比 | > 100 | 典型索引缺失，优先处理 |
| 慢查询总数/小时 | > 50 | 系统级性能问题，需全面排查 |
| 慢查询总数/天 | > 200 | 告警并分析高频慢查询模板 |
| 特定表碎片率 | > 10MB | 执行 `OPTIMIZE TABLE` |

**建议 cron 任务（每天凌晨生成报告）：**

```cron
# 每天凌晨 3 点执行慢查询分析
0 3 * * * pt-query-digest /var/log/mysql/mysql-slow.log --since='24h' > /var/log/mysql/slow-report-$(date +\%Y\%m\%d).txt
```

---

## 4. 熔断与重试机制

### 4.1 HttpClient Timeout 配置

**当前各服务 HttpClient 超时配置：**

| 服务 | HttpClient | Timeout |
|------|-----------|---------|
| TicketService → PersonService | PersonService | 10s |
| TicketService → MasterDataService | MasterDataService | 10s |
| TicketService → DispatchService | DispatchService | 10s |

**超时配置建议：**

- **读操作（GET）：** 5-10s
- **写操作（POST/PUT）：** 10-30s
- **批量操作：** 60s 以上建议使用后台队列

### 4.2 Polly 熔断器配置

**已实现于 GatewayService：**

| 策略 | 配置 | 说明 |
|------|------|------|
| **熔断器** | `failureThreshold=0.5`, `minimumThroughput=10`, `durationOfBreak=30s` | 30% 请求失败后熔断 30 秒 |
| **重试策略** | `retryCount=3`, 指数退避 `2s→4s→8s` | 服务不可用时自动重试 3 次 |
| **超时策略** | `Timeout=30s` | 单次请求最多 30 秒 |

**熔断器工作流程：**

```
正常 → (失败率 > 50%, 最小请求数 10) → 熔断开放(30s)
     → 熔断半开(尝试恢复) → 成功则关闭，失败则继续开放
```

### 4.3 重试策略建议

**GatewayService 当前重试条件（仅对 503/504/408 重试）：**

```csharp
// 仅在以下状态码时重试
r.StatusCode == ServiceUnavailable  // 503
r.StatusCode == GatewayTimeout       // 504
r.StatusCode == RequestTimeout      // 408
```

**扩展建议（可选）：**

| 状态码 | 是否重试 | 退避策略 |
|--------|---------|---------|
| 500 Internal Server Error | 不重试（代码 bug） | — |
| 502 Bad Gateway | 重试 1 次 | 立即 |
| 503 Service Unavailable | 重试 3 次 | 指数 2s→4s→8s |
| 504 Gateway Timeout | 重试 3 次 | 指数 2s→4s→8s |
| 408 Request Timeout | 重试 2 次 | 固定 1s |
| 429 Too Many Requests | 重试 1 次 | 看 Retry-After header |

**不建议重试的场景：**
- `POST /tickets`（创建工单）— 幂等性问题，可能产生重复工单
- `PUT /tickets/{id}/dispatch`（派单）— 状态机操作，重复执行有副作用
- 登录/登出等有副作用的写操作

---

## 5. 执行检查清单

### 上线前检查

- [ ] `add_indexes.sql` 已在测试环境执行并验证
- [ ] 索引创建后核心查询使用 `EXPLAIN` 验证走索引
- [ ] 慢查询日志阈值 `long_query_time=0.2` 已配置
- [ ] `pt-query-digest` 工具已安装或等效分析脚本已准备
- [ ] 连接池参数已在各服务 `Program.cs` 中调整
- [ ] `Max Pool Size` 根据实例数分配，总连接数不超过 MySQL `max_connections`
- [ ] 熔断器配置已确认（GatewayService）
- [ ] 告警阈值已配置或已告知 DBA

### 上线后检查（24小时内）

- [ ] 慢查询日志中无新增 > 1s 的查询
- [ ] 连接池无连接耗尽（`max_connections` 未达到）
- [ ] 无大量 `Connection timeout` 错误
- [ ] 游标分页替代 OFFSET 分页的 API 改造已上线
- [ ] 碎片率检查（表大小 > 10MB 且 `Data_free` > 1MB 的表需 `OPTIMIZE`）

### 关键 SQL 监控

```sql
-- 1. 检查当前连接数
SHOW STATUS LIKE 'Threads_connected%';

-- 2. 检查慢查询数量（对比基线）
SHOW GLOBAL STATUS LIKE 'Slow_queries';

-- 3. 检查锁等待
SELECT * FROM information_schema.INNODB_LOCK_WAITS;

-- 4. 检查当前执行的慢查询（实时）
SELECT * FROM information_schema.processlist
WHERE command != 'Sleep' AND time > 5;
```

---

## 附录：相关文件索引

| 文件 | 用途 |
|------|------|
| `scripts/db_optimization/add_indexes.sql` | 索引优化脚本（19个索引） |
| `scripts/db_optimization/slow_query_log.sql` | 慢查询日志配置脚本 |
| `scripts/db_optimization/slow_query_analysis.sql` | 慢查询分析脚本 |
| `src/WO.Property.TicketService/Program.cs` | 工单服务（连接池配置、HttpClient） |
| `src/WO.Property.MasterDataService/Program.cs` | 主数据服务（连接池配置） |
| `src/WO.Property.PersonService/Program.cs` | 人员服务（连接池配置） |
| `src/WO.Property.GatewayService/Authentication/DownstreamPolicyExtensions.cs` | Polly 熔断和重试策略 |