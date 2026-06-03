# MySQL 数据库优化方案 - 多项目多用户架构

**版本**: v1.0  
**日期**: 2026-05-27  
**状态**: 已确认  
**负责人**: 芦苇（软件负责人）

---

## 1. 背景与目标

### 1.1 当前问题

- MySQL 最大连接数不足（默认 151）
- 连接未及时释放，导致 "Too many connections" 错误
- 单数据库无隔离，无法支撑多项目扩展

### 1.2 设计目标

| 目标 | 指标 |
|------|------|
| 支持项目数 | 100+ |
| 支持并发用户 | 5000+ |
| 服务可用性 | 99.9% |
| 数据隔离 | 项目级 |

---

## 2. 架构方案

### 2.1 系统架构图

```
┌─────────────────────────────────────────────────────┐
│                   应用层                            │
│  14 个微服务 (连接池上限 10/服务)                    │
└─────────────────┬───────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────────┐
│           Connection Proxy (cproxy)                │
│           • 连接池集中管理                          │
│           • 智能路由 (项目 → 数据库)                  │
│           • 连接复用 (复用率 80%+)                   │
└─────────────────┬───────────────────────────────────┘
                  │
    ┌─────────────┼─────────────┐
    ▼             ▼             ▼
┌───────┐   ┌───────┐   ┌───────┐
│DB: P1 │   │DB: P2 │   │DB: Pn │
│(项目1)│   │(项目2)│   │(项目n)│
└───────┘   └───────┘   └───────┘
```

### 2.2 连接数计算

| 来源 | 连接数 | 说明 |
|------|--------|------|
| 每服务连接池 | 10 | 可配置 |
| 14 服务 | 140 | 正常运行 |
| admin-portal | 20 | 管理后台额外 |
| 小程序连接 | 50 | 用户请求 |
| **总需求** | **~210** | 留有 90% 余量 |
| **MySQL 配置** | **2000** | 预留扩展空间 |

---

## 3. MySQL 配置方案

### 3.1 配置文件 (my.cnf)

**路径**: `/usr/local/etc/my.cnf`

```ini
[mysqld]
# ============ 连接配置 ============
max_connections = 2000           # 支持 2000 并发
wait_timeout = 600              # 10分钟超时
interactive_timeout = 600       # 交互超时
max_connect_errors = 999999      # 防止连接阻塞

# ============ 性能优化 ============
innodb_buffer_pool_size = 2G    # 2GB 缓冲池 (根据内存调整)
innodb_log_file_size = 512M     # 日志文件
innodb_flush_log_at_trx_commit = 2  # 平衡安全与性能
innodb_flush_method = O_DIRECT   # 直接刷新

# ============ 连接优化 ============
max_allowed_packet = 64M        # 大包
connection_timeout = 30         # 连接超时
skip_name_resolve = ON          # 跳过 DNS 解析

# ============ 查询优化 ============
tmp_table_size = 256M
max_heap_table_size = 256M
sort_buffer_size = 4M
read_buffer_size = 4M
join_buffer_size = 4M

# ============ 字符集 ============
character-set-server = utf8mb4
collation-server = utf8mb4_unicode_ci
init_connect = 'SET NAMES utf8mb4'

[client]
default-character-set = utf8mb4
```

### 3.2 参数说明

| 参数 | 值 | 说明 |
|------|-----|------|
| `max_connections` | 2000 | 支持大量并发 |
| `innodb_buffer_pool_size` | 2G | 热点数据缓存 |
| `wait_timeout` | 600 | 10分钟无操作断开 |
| `skip_name_resolve` | ON | 跳过 DNS 解析加速 |
| `innodb_flush_log_at_trx_commit` | 2 | 平衡安全与性能 |

---

## 4. .NET 服务连接配置

### 4.1 ConnectionStrings 配置

**路径**: 各服务的 `appsettings.json`

```json
{
  "ConnectionStrings": {
    "CenterDb": "Server=localhost;Port=3306;Database=center_db;User=root;Password=;CharSet=utf8mb4;Pooling=true;MaximumPoolSize=10;MinPoolSize=2;ConnectionLifeTime=300;",
    "Default": "Server=localhost;Port=3306;Database=wo_property;User=root;Password=;CharSet=utf8mb4;Pooling=true;MaximumPoolSize=10;MinPoolSize=2;ConnectionLifeTime=300;"
  }
}
```

### 4.2 连接池参数说明

| 参数 | 值 | 说明 |
|------|-----|------|
| `Pooling` | true | 启用连接池 |
| `MaximumPoolSize` | 10 | 每服务最多 10 连接 |
| `MinPoolSize` | 2 | 保持最少 2 连接复用 |
| `ConnectionLifeTime` | 300 | 5分钟回收，避免死连接 |

---

## 5. 多项目隔离策略

### 5.1 隔离阶段规划

#### 阶段 1：逻辑隔离 (当前)

```
wo_property 数据库
├── 项目A数据 (tenant_id = 1)
├── 项目B数据 (tenant_id = 2)
└── 所有项目共享数据库，但逻辑分区
```

**适用场景**: 10 个项目以内，并发用户 500 以内

#### 阶段 2：分库隔离 (中期)

```
center_db (租户主库)
├── wo_projectA (项目A独立库)
├── wo_projectB (项目B独立库)
└── wo_property_main (主项目库)
```

**适用场景**: 10-50 个项目，并发用户 1000 以内

#### 阶段 3：数据库集群 (长期)

```
ProxySQL (读写分离)
├── 主库 (写)
├── 从库 x3 (读)
└── 分片库 (大数据量)
```

**适用场景**: 50+ 个项目，并发用户 2000+

### 5.2 租户路由中间件

```csharp
// TenantRoutingMiddleware.cs
public class TenantRoutingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ITenantDbFactory _tenantDbFactory;

    public async Task InvokeAsync(HttpContext context)
    {
        // 从 Header 获取租户代码
        var tenantCode = context.Request.Headers["X-Tenant-Code"].FirstOrDefault() ?? "default";

        // 设置数据库路由
        context.Items["TenantCode"] = tenantCode;

        await _next(context);
    }
}
```

---

## 6. 部署步骤

### 6.1 执行命令

```bash
# 1. 创建 MySQL 配置
sudo tee /usr/local/etc/my.cnf << 'EOF'
[mysqld]
max_connections = 2000
wait_timeout = 600
interactive_timeout = 600
max_connect_errors = 999999
innodb_buffer_pool_size = 2G
innodb_log_file_size = 512M
innodb_flush_log_at_trx_commit = 2
innodb_flush_method = O_DIRECT
max_allowed_packet = 64M
connection_timeout = 30
skip_name_resolve = ON
tmp_table_size = 256M
max_heap_table_size = 256M
sort_buffer_size = 4M
read_buffer_size = 4M
join_buffer_size = 4M
character-set-server = utf8mb4
collation-server = utf8mb4_unicode_ci
init_connect = 'SET NAMES utf8mb4'

[client]
default-character-set = utf8mb4
EOF

# 2. 重启 MySQL
sudo /usr/local/opt/mysql/bin/mysqladmin shutdown 2>/dev/null || true
sleep 3
/usr/local/opt/mysql/bin/mysqld_safe --datadir=/usr/local/var/mysql &

# 3. 验证
sleep 5
mysql -u root -e "SHOW VARIABLES LIKE 'max_connections';"
mysql -u root -e "SHOW VARIABLES LIKE 'innodb_buffer_pool_size';"
```

### 6.2 验证清单

| 检查项 | 预期结果 |
|--------|----------|
| max_connections | 2000 |
| innodb_buffer_pool_size | 2147483648 (2GB) |
| wait_timeout | 600 |
| skip_name_resolve | ON |

---

## 7. 容量评估

### 7.1 支持能力

| 指标 | 当前配置 | 最大配置 |
|------|----------|----------|
| MySQL max_connections | 2000 | 10000 |
| 支持服务数 | 100+ | 500+ |
| 支持并发用户 | 5000+ | 20000+ |
| 每服务连接池 | 10 | 10-20 |
| 数据库数量 | 1-10 | 50+ |

### 7.2 性能基准

| 场景 | 响应时间 | QPS |
|------|----------|-----|
| 工单查询 | < 100ms | 500+ |
| 工单创建 | < 200ms | 200+ |
| 人员查询 | < 50ms | 1000+ |

---

## 8. 监控与告警

### 8.1 关键指标

```sql
-- 连接数监控
SHOW STATUS LIKE 'Threads_connected';
SHOW STATUS LIKE 'Max_used_connections';

-- 查询缓存
SHOW STATUS LIKE 'Qcache%';

-- 慢查询
SHOW VARIABLES LIKE 'slow_query_log';
SHOW VARIABLES LIKE 'long_query_time';
```

### 8.2 告警阈值

| 指标 | 警告 | 严重 |
|------|------|------|
| 连接使用率 | > 70% | > 90% |
| 慢查询数 | > 10/min | > 50/min |
| 缓冲池命中率 | < 90% | < 80% |

---

## 9. 未来扩展路径

```
阶段1 (当前)
└── 单库 + 逻辑隔离 (tenant_id)
    └── 支持: 10 项目, 500 用户

阶段2 (中期)
└── 分库 (每项目独立数据库)
    └── 支持: 50 项目, 2000 用户

阶段3 (长期)
└── 数据库集群 (主从 + 分片)
    └── 支持: 100+ 项目, 5000+ 用户
```

---

## 10. 确认记录

| 日期 | 确认人 | 备注 |
|------|--------|------|
| 2026-05-27 | 芦苇 | 完成架构设计 |
| 2026-05-27 | 强哥 | 确认方案可行 |

---

**文档状态**: ✅ 已确认  
**下次评审**: 2026-06-01