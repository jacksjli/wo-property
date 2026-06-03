# WO 物业管理软件 - 架构梳理报告

**日期**: 2026-05-25
**状态**: 🔴 需要决策
**目标**: 确定单租户多项目架构

---

## 一、当前架构状态

### 1.1 设计目标 vs 实际实现

| 维度 | 目标（单租户多项目） | 当前实际 |
|------|---------------------|---------|
| 数据库 | 统一数据库 `wo_property` | `wo_property` + 独立项目库 |
| 项目标识 | `project_code`（如 YGHY001） | 登录返回 YGHY001/YGXY001 |
| 项目映射 | `project-mapping.json` | 定义的是 jinxiu/yangguang/xingfuli |
| 中间件 | `ProjectRoutingMiddleware` | 仍使用 `TenantRoutingMiddleware` |
| 端口配置 | `config/ports.json` | ✅ 已统一管理 |

### 1.2 数据库现状

```
MySQL 已创建数据库:
├── wo_property          # 主数据库
├── project_center       # 项目中心库（未启动 CenterService）
├── project_jinxiu       # 独立项目库
├── project_yangguang    # 独立项目库
└── project_xingfuli     # 独立项目库
```

### 1.3 项目标识不一致问题

**project-mapping.json 定义:**
```
jinxiu → project_jinxiu
yangguang → project_yangguang
xingfuli → project_xingfuli
```

**AuthService 登录返回:**
```
id=5, code=YGHY001 (阳光花园小区)
id=6, code=YGXY001 (阳光新苑)
```

**问题**: 两套 project_code 完全不一致，无法建立映射。

---

## 二、需要清理的多租户残留

### 2.1 旧数据库（应删除或废弃）
- `project_jinxiu` — 与 `jinxiu` code 对应，但前端用的是 YGHY001
- `project_yangguang` — 与 `yangguang` code 对应
- `project_xingfuli` — 与 `xingfuli` code 对应

### 2.2 未运行的中心服务
- **CenterService (5016)** — 未启动，项目中心未生效
- `project_center` 数据库 — 数据存在但服务未连接

### 2.3 残留的 TenantDbFactory

以下服务包含旧的 TenantDbFactory（应替换为 ProjectDbFactory）:
- TicketService (5102)
- DispatchService (5241)
- StatisticsService (5250)
- MaterialService (5504)

### 2.4 过时的文档
- `docs/PHASE0_MULTI_TENANT_ARCHITECTURE.md` — 描述多租户分库架构
- `docs/MODULES/ticket/API.md` — 描述单租户 API

---

## 三、核心问题：项目标识不匹配

### 3.1 三个不同的项目标识系统

| 系统 | 来源 | 示例 |
|------|------|------|
| project_code (登录) | AuthService 查询 user_projects + projects | YGHY001, YGXY001 |
| project_code (配置) | project-mapping.json | jinxiu, yangguang, xingfuli |
| database_name | project-mapping.json | project_jinxiu, project_yangguang |

### 3.2 登录后 X-Project 应该传什么？

当前代码中：
```python
# 前端登录后获取 projects
projects = [{'id': 5, 'code': 'YGHY001', 'name': '阳光花园小区'}, ...]

# 后续请求带 X-Project: 'YGHY001'
# 但中间件 TenantRoutingMiddleware 检查:
#   - project-mapping.json 中没有 YGHY001
#   - 只有 jinxiu/yangguang/xingfuli
#   → 报错 "租户不存在"
```

---

## 四、决策点

### 决策 1: 项目标识系统统一

**选项 A**: 以登录返回的 `project_code` 为准（YGHY001/YGXY001）
- 修改 `project-mapping.json`，将 project_code 改为 YGHY001/YGXY001
- 清理旧的 jinxiu/yangguang/xingfuli 配置

**选项 B**: 以 `project-mapping.json` 的 code 为准（jinxiu/yangguang/xingfuli）
- 需要修改 AuthService 登录返回的 project_code
- 前端需要同步修改

**选项 C**: 完全废弃 project-mapping.json，改用数据库动态加载
- CenterService 读取 `project_center.projects` 表获取映射
- 各服务在运行时请求 CenterService 获取 project_code → database_name 映射

### 决策 2: 独立项目数据库的处理

**选项 A**: 全部合并到 `wo_property`
- 迁移 `project_jinxiu/yangguang/xingfuli` 数据到 `wo_property`
- 删除独立数据库
- 彻底实现单租户多项目

**选项 B**: 保持独立数据库（多数据库模式）
- 各项目有独立数据库
- 通过 `project_code` → `database_name` 映射路由
- 修改 TenantDbFactory 支持此模式

### 决策 3: 中间件命名和实现

**当前**: `TenantRoutingMiddleware` + `TenantDbFactory`
**目标**: `ProjectRoutingMiddleware` + `ProjectDbFactory`

---

## 五、建议的清理步骤

### Step 1: 统一项目标识（高优先级）

修改 `config/project-mapping.json`:
```json
{
  "projects": {
    "YGHY001": { "databaseName": "project_jinxiu", "displayName": "阳光花园小区" },
    "YGXY001": { "databaseName": "project_yangguang", "displayName": "阳光新苑" }
  }
}
```

### Step 2: 启动 CenterService（高优先级）

- CenterService (5016) 应该管理 `project_center` 数据库
- 各服务启动时从 CenterService 获取项目映射

### Step 3: 清理残留代码

- 重命名 `TenantRoutingMiddleware` → `ProjectRoutingMiddleware`
- 重命名 `TenantDbFactory` → `ProjectDbFactory`
- 删除旧的 `/api/tenant/*` 路由，改用 `/api/project/*`

### Step 4: 清理文档

- 废弃 `docs/PHASE0_MULTI_TENANT_ARCHITECTURE.md`
- 更新 `docs/AUDIT_LOG.md` 中的多租户描述
- 创建新的单租户多项目架构文档

---

## 六、立即可执行的操作

| 操作 | 优先级 | 影响 |
|------|--------|------|
| 1. 更新 `project-mapping.json` 的 code 为 YGHY001/YGXY001 | P0 | 解决 X-Project 路由问题 |
| 2. 启动 CenterService (5016) | P0 | 使项目中心生效 |
| 3. 更新 AuthService 登录返回的 project_code 格式 | P1 | 与 project-mapping.json 一致 |
| 4. 清理 TenantRoutingMiddleware 中的旧逻辑 | P2 | 减少混淆 |
| 5. 文档更新 | P2 | 正确描述架构 |

---

## 七、风险

- 在清理过程中，可能影响正在使用系统的用户
- 独立项目数据库和统一 `wo_property` 的数据同步问题
- CenterService 未启动时，各服务无法动态获取项目映射