# 审计报告 — Phase 0 多物业分库改造 — 2026-05-18

## 审计概述

| 项目 | 内容 |
|------|------|
| **审计时间** | 2026-05-18 09:00 |
| **审计范围** | Phase 0 feature/multi-tenant 分支（AuthService + TicketService） |
| **分支状态** | feature/multi-tenant，4个 commit，本地未推送到远程 |
| **评级** | 🟡 观察 |

---

## 1. API契约检查

### 1.1 AuthService (端口 5106)

| 接口 | 文档描述 | 实际行为 | 偏差 | 严重度 |
|------|---------|---------|------|--------|
| POST /api/auth/login | 单租户登录（仅 username/password） | 新增多 tenantCode 参数 | ⚠️ 文档缺失 | 低 |
| JWT Claims | 无 tenant 字段 | 新增 tenant_id, tenant_code, project_ids | ⚠️ 文档缺失 | 中 |

**说明：** AuthService 尚未有文档描述多租户改造，docs/ 下无 Phase 0 相关文档。

### 1.2 TicketService (端口 5102)

| 接口 | 文档描述 | 实际行为 | 偏差 | 严重度 |
|------|---------|---------|------|--------|
| GET /api/tickets | 单租户工单列表 | 新多租户端点 GET /api/tenant/tickets | ⚠️ 文档缺失 | 高 |
| POST /api/tickets | 单租户创建工单 | 新多租户端点 POST /api/tenant/tickets | ⚠️ 文档缺失 | 高 |
| 字段映射 | 文档描述 status/name 等英文字段 | 实际为 status/title/priority/category（与表结构对齐） | ⚠️ 文档过时 | 高 |

**说明：** `docs/MODULES/ticket/API.md` 描述的是 main 分支单租户 API，与 feature/multi-tenant 分支实际 API 完全不同。

---

## 2. 决策追溯检查

| 决策 | 文档位置 | 执行状态 | 说明 |
|------|---------|---------|------|
| MySQL 统一数据库 | MEMORY.md | ✅ 已执行 | 18个服务从 PostgreSQL 迁移到 MySQL |
| AuthService 多租户改造 | MEMORY.md | ✅ 已执行 | JWT 新增 claim，UseUrls 5106 |
| TicketService TenantDbFactory | 无文档 | ✅ 已执行 | AsyncLocal 租户上下文，动态库切换 |
| TenantDbContext 动态路由 | 无文档 | ✅ 已执行 | TenantRoutingMiddleware JWT 解析 |
| 分支策略（独立分支） | MEMORY.md | ✅ 已执行 | feature/multi-tenant，不影响 main |

**问题：** 核心架构决策（TenantDbFactory、动态库切换）无设计文档，存在知识传承风险。

---

## 3. 安全检查

| 检查项 | 预期 | 实际 | 结果 |
|--------|------|------|------|
| JWT 无 tenant_code 访问 | 拒绝/降级 | 允许匿名访问（跳过中间件） | 🟢 合规 |
| 租户隔离 | 数据完全隔离 | tenant_a/tenant_b 数据库级隔离 | 🟢 合规 |
| 密码锁定 | 失败5次锁定 | auth service 内存锁定 15 分钟 | 🟢 合规 |
| 账户锁定状态 | MySQL 中 status='Active' | unlock 后可登录 | 🟢 合规 |

---

## 4. 不一致问题清单

| # | 问题描述 | 严重度 | 修复建议 |
|---|---------|--------|---------|
| 1 | **Phase 0 无设计文档** — docs/ 下无多物业分库相关文档 | 高 | 创建 `docs/PHASE0_MULTI_TENANT_ARCHITECTURE.md` |
| 2 | **docs/MODULES/ticket/API.md 过时** — 描述单租户 API，但代码已是多租户 | 高 | 标注 DEPRECATED 或更新为多租户 API |
| 3 | **admin_a 账户锁定问题** — 错误密码测试导致内存锁定 15 分钟，影响测试效率 | 低 | 测试脚本应使用 tech_a 而非 admin_a |
| 4 | **feature/multi-tenant 未推送 GitHub** — 4个 commit 本地，远程不可见 | 中 | 等待网络恢复后推送 |
| 5 | **TicketService 旧 Controller** — 单租户 TicketController (5002) 和多租户 TenantTicketController (5102) 同时存在 | 中 | Phase 1 需决定旧接口是否废弃 |

---

## 5. 配置一致性

| 检查项 | 配置值 | 实际值 | 结果 |
|--------|--------|--------|------|
| AuthService 端口 | 5106（Phase 0） | 5106 | 🟢 |
| TicketService 端口 | 5102（Phase 0） | 5102 | 🟢 |
| center_db 连接 | 127.0.0.1:3306 | 127.0.0.1:3306 | 🟢 |
| tenant 数据库 | tenant_a, tenant_b | 存在且有数据 | 🟢 |

---

## 6. 测试覆盖

| 维度 | 覆盖率 | 说明 |
|------|--------|------|
| 单元测试 | ✅ 13个通过 | TenantDbFactory, Middleware, Controller |
| 集成测试 | ✅ 16个场景通过 | scripts/phase0-integration-test.sh |
| API 手动验证 | ✅ 已做 | curl 多次验证租户隔离 |

---

## 7. 下周行动项

- [ ] **高优先级**：编写 Phase 0 架构文档（docs/PHASE0_MULTI_TENANT_ARCHITECTURE.md）
- [ ] **高优先级**：标注 `docs/MODULES/ticket/API.md` 为 DEPRECATED
- [ ] **中优先级**：推送 feature/multi-tenant 到 GitHub
- [ ] **中优先级**：Phase 1 规划文档（其他 12 个服务改造路线图）
- [ ] **低优先级**：修复 integration test 使用 tech_a 避免锁定

---

## 审计结论

**评级：🟡 观察**

Phase 0 核心代码质量良好，租户隔离验证通过，但设计文档严重滞后于实现。当前代码与文档存在以下断层：

1. **Phase 0 架构决策无文档记录** — 知识集中在 MEMORY.md，团队其他成员无法查阅
2. **旧 API 文档未标注过时** — 可能导致后续开发者误用旧 API 契约
3. **feature/multi-tenant 分支未推送** — CI/CD 无法覆盖新代码

建议本周优先补充架构文档，再推进 Phase 1。

---

_记录人：🪽的芦苇_
_审计时间：2026-05-18 09:00_
_下次审计：2026-05-25_