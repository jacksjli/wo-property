# 数据库迁移文档

> **版本**：v1.0
> **目标**：所有 14 个微服务统一使用 MySQL，淘汰 SQLite
> **最后更新**：2026-05-05

---

## 1. 迁移策略

### 1.1 推荐策略：双库并行 + DNS 切换

```
Phase 0：准备阶段（2天）
  1. 在测试环境搭建 MySQL
  2. 编写迁移脚本（SQLite → MySQL）
  3. 测试迁移脚本
  4. 验证数据完整性

Phase 1：PersonService + MasterDataService 先迁移（3天）
  1. 创建 PersonService（MySQL）
  2. 创建 MasterDataService（MySQL）
  3. 迁移初始数据
  4. 验证通过后，TicketService 开始调用

Phase 2：其他服务逐一迁移（1周）
  按依赖顺序，逐一迁移：
  1. TicketService
  2. MaterialService
  3. ContractService
  4. FinanceService（已经是 MySQL，检查是否需要迁移表结构）
  5. 其他服务...

Phase 3：SQLite 清理（1天）
  所有服务验证通过后，删除旧的 SQLite 文件
```

---

## 2. 迁移脚本规范

### 2.1 脚本命名

```
migrations/
  ├── 001_persons_initial.sql
  ├── 002_buildings_initial.sql
  ├── 003_tickets_initial.sql
  ├── ...
```

### 2.2 迁移脚本模板

```sql
-- migration_003_tickets_initial.sql
-- 将 TicketService 的 SQLite 数据迁移到 MySQL

-- 1. 创建表结构（MySQL 语法）
CREATE TABLE IF NOT EXISTS `tickets` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `TicketCode` VARCHAR(50) NOT NULL,
    `Title` VARCHAR(200) NOT NULL,
    `Description` TEXT,
    `Category` VARCHAR(50),
    `Priority` VARCHAR(20) NOT NULL DEFAULT 'Medium',
    `Status` VARCHAR(20) NOT NULL DEFAULT 'New',
    `CreatorPersonId` INT,
    `AssigneePersonId` INT,
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME,
    PRIMARY KEY (`Id`),
    UNIQUE INDEX `IX_tickets_TicketCode` (`TicketCode`),
    INDEX `IX_tickets_Status` (`Status`),
    INDEX `IX_tickets_Priority` (`Priority`),
    INDEX `IX_tickets_CreatedAt` (`CreatedAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 2. 数据验证
SELECT COUNT(*) FROM tickets;
```

---

## 3. 数据完整性验证

迁移后必须验证：

```sql
-- 验证记录数一致
SELECT COUNT(*) FROM tickets;  -- MySQL 和 SQLite 对比

-- 验证关键字段完整性
SELECT COUNT(*) FROM tickets WHERE TicketCode IS NULL;  -- 应为 0

-- 验证外键关系
SELECT COUNT(*) FROM tickets t 
LEFT JOIN persons p ON t.CreatorPersonId = p.Id 
WHERE p.Id IS NULL AND t.CreatorPersonId IS NOT NULL;  -- 应为 0
```

---

## 4. 各服务迁移顺序

```
优先级高（先迁移）：
  PersonService (5018)      ← 所有服务依赖
  MasterDataService (5019)  ← 所有服务依赖

优先级中：
  TicketService (5002)      ← 依赖 PersonService
  MaterialService (5004)     ← 独立，可并行
  ContractService (5008)    ← 依赖 PersonService
  DeviceService (5007)        ← 依赖 MasterDataService

优先级低（最后迁移）：
  KeyService, VisitorService, NotificationService, 
  InspectionService, ComplaintService, StatisticsService,
  MobileService, AuthService, DispatchService
```

---

## 5. 验证检查清单

每个服务迁移完成后，必须通过以下检查：

```
□ 数据量验证：MySQL 记录数 == 迁移前 SQLite 记录数
□ 字段验证：关键字段（Code, Name 等）无 NULL（除允许外）
□ 外键验证：外键引用的 ID 在主表存在
□ 索引验证：高频查询字段已建索引
□ 性能验证：单表查询 < 100ms，全表扫描 < 1s
□ 连接验证：服务能成功连接 MySQL
□ 读写验证：CRUD 操作正常
□ 中文验证：中文字符正确显示
□ 特殊字符验证：特殊字符（' " \）正常处理
```

---

## 6. 风险与应对

| 风险 | 影响 | 应对 |
|------|------|------|
| 迁移失败数据丢失 | 业务中断 | 迁移前完整备份 |
| 迁移时间过长 | 服务不可用 | 分批迁移，每批控制在 30 分钟内 |
| 外键关系断裂 | 数据不一致 | 迁移后立即验证外键 |
| 性能下降 | 用户体验差 | 提前添加必要索引 |
| MySQL 单点故障 | 全系统崩溃 | Docker Compose 配置主从复制 |

---

**文档版本**：v1.0
**作者**：数据库工程师
**审核**：软件负责人
**状态**：待评审
