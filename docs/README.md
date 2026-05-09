# WO 物业管理软件文档

> **项目**：WO-Property-Management
> **版本**：v1.0
> **更新日期**：2026-05-06

---

## 文档目录

### ARCHITECTURE - 全局架构规范

| 文档 | 说明 | 状态 |
|------|------|------|
| [API_DESIGN.md](./ARCHITECTURE/API_DESIGN.md) | API 设计标准、URL规范、响应格式 | 待评审 |
| [SERVICE_COMMUNICATION.md](./ARCHITECTURE/SERVICE_COMMUNICATION.md) | 服务间通信协议、调用规范 | 待评审 |
| [DATABASE_DESIGN.md](./ARCHITECTURE/DATABASE_DESIGN.md) | 数据库设计规范、表结构 | 待评审 |
| [FIELD_NAMING.md](./ARCHITECTURE/FIELD_NAMING.md) | 字段命名规范、来源映射 | 待评审 |
| [SECURITY.md](./ARCHITECTURE/SECURITY.md) | 安全设计规范、认证方案 | 待评审 |
| [MODULE_GUIDELINES.md](./ARCHITECTURE/MODULE_GUIDELINES.md) | **模块化设计原则（新增/删除/修改）** | **已确认** |

---

### STANDARDS - 代码/工程规范

| 文档 | 说明 | 状态 |
|------|------|------|
| [CODING_STANDARDS.md](./STANDARDS/CODING_STANDARDS.md) | C#/Vue 代码编写规范 | 待评审 |
| [GIT_WORKFLOW.md](./STANDARDS/GIT_WORKFLOW.md) | Git 分支策略、提交流程 | 待评审 |
| [CODE_REVIEW.md](./STANDARDS/CODE_REVIEW.md) | Code Review 规范、检查清单 | 待评审 |

---

### MODULES - 模块级规范

#### 工单模块 (ticket)
| 文档 | 说明 | 状态 |
|------|------|------|
| [MODULES/ticket/DESIGN.md](./MODULES/ticket/DESIGN.md) | 工单模块设计、编号规范 | 已确认 |
| [MODULES/ticket/API.md](./MODULES/ticket/API.md) | 工单 API 定义 | 已确认 |
| [MODULES/ticket/FIELDS.md](./MODULES/ticket/FIELDS.md) | 工单字段定义 | 已确认 |
| [MODULES/ticket/STATUS_FLOW.md](./MODULES/ticket/STATUS_FLOW.md) | 工单状态流转图 | 已确认 |

#### 人员模块 (person)
| 文档 | 说明 | 状态 |
|------|------|------|
| [MODULES/person/DESIGN.md](./MODULES/person/DESIGN.md) | 人员模块设计 | 待定 |

#### 设备模块 (device)
| 文档 | 说明 | 状态 |
|------|------|------|
| [MODULES/device/FIELDS.md](./MODULES/device/FIELDS.md) | 设备字段定义 | 待评审 |

#### 其他模块
| 模块 | 状态 |
|------|------|
| material | 规划中 |
| complaint | 规划中 |
| key | 规划中 |
| visitor | 规划中 |
| payment | 规划中 |
| contract | 规划中 |
| masterdata | 规划中 |
| finance | 规划中 |
| announcement | 规划中 |
| inspection | 规划中 |
| parking | 规划中 |
| dispatch | 规划中 |

---

### PROCESS - 跨模块流程

| 文档 | 说明 | 状态 |
|------|------|------|
| [PROCESS/DEPLOYMENT.md](./PROCESS/DEPLOYMENT.md) | 部署文档、架构图 | 待评审 |
| [PROCESS/MIGRATION.md](./PROCESS/MIGRATION.md) | 数据库迁移方案 | 待评审 |
| [PROCESS/TESTING.md](./PROCESS/TESTING.md) | 测试策略、测试用例 | 待评审 |

---

## 文档更新记录

| 日期 | 更新内容 | 更新人 |
|------|---------|--------|
| 2026-05-06 | 重构文档架构，从混合格式改为分层架构 | 软件负责人 |

---

## 旧文档（待清理）

以下旧文档将在确认迁移完成后删除：

```
docs/design/TICKET_SERVICE_ARCHITECTURE_v1.0.md  → 已迁移到 MODULES/ticket/
docs/field-management/FIELD_CLASSIFICATION.md      → 已拆分到 ARCHITECTURE/FIELD_NAMING.md 和 MODULES/*/FIELDS.md
docs/architecture/service-communication.md         → 已迁移到 ARCHITECTURE/
docs/architecture/api-design.md                   → 已迁移到 ARCHITECTURE/
docs/architecture/database-design.md               → 已迁移到 ARCHITECTURE/
docs/architecture/security.md                     → 已迁移到 ARCHITECTURE/
docs/architecture/mysql-migration.md              → 已迁移到 PROCESS/
docs/architecture/implementation-plan.md          → 内容已过时，待清理
```

---

## 贡献指南

### 文档更新流程

1. 在对应模块目录下创建/修改文档
2. 更新本 README.md 的文档索引
3. 提交 PR 进行 Code Review
4. 审核通过后合并

### 文档命名规范

```
✓ 正确：
  - DESIGN.md (模块设计)
  - API.md (API 定义)
  - FIELDS.md (字段定义)
  - STATUS_FLOW.md (状态流转)
  - README.md (模块索引)

✗ 错误：
  - 模块设计_v1.0.md (包含版本号)
  - 2026-05-06-设计.md (包含日期)
```

---

## 联系方式

如有文档问题，请联系软件架构师或软件负责人。
