# 工单模块设计

> **版本**：v1.0
> **服务**：TicketService (5002)
> **状态**：已确认

---

## 一、概述

工单模块是 WO 物业管理软件的核心模块，负责物业维修、巡检、安保等各类工单的**全生命周期管理**。

### 1.1 核心职责

```
TicketService (5002) 职责：
  - 工单创建、查询、修改
  - 工单状态流转
  - 调用 DispatchService 进行智能派单
  - 调用 PersonService 获取人员信息
```

### 1.2 服务依赖

```
TicketService 依赖：
  - PersonService (5018)：获取人员信息（创建人、指派人）
  - MasterDataService (5019)：获取枚举值（工单类型、优先级等）
  - DispatchService (5003)：派单服务
```

---

## 二、工单编号规范

### 2.1 格式定义

```
格式: WO-YYYYMMDD-XXXX
示例: WO-20260506-0001

说明:
  - WO: 前缀，固定
  - YYYYYMMDD: 8位日期
  - XXXX: 4位流水号，每日从0001开始，不足4位补0
```

### 2.2 生成规则

```
1. 同一日期内的工单编号连续递增
2. 每日凌晨 00:00 重置流水号
3. 编号在创建时生成，不允许修改
```

---

## 三、数据库模型

### 3.1 工单主表 (Tickets)

```sql
CREATE TABLE tickets (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    ticket_no VARCHAR(20) NOT NULL UNIQUE COMMENT '工单编号 WO-YYYYMMDD-XXXX',
    title VARCHAR(200) NOT NULL COMMENT '工单标题',
    description TEXT COMMENT '工单描述',
    category VARCHAR(50) COMMENT '工单分类',
    priority INT DEFAULT 2 COMMENT '优先级 1=紧急 2=高 3=中 4=低',
    status VARCHAR(20) NOT NULL DEFAULT 'Created' COMMENT '状态',
    creator_id BIGINT NOT NULL COMMENT '创建人ID',
    assignee_id BIGINT COMMENT '处理人ID',
    location VARCHAR(500) COMMENT '位置',
    images JSON COMMENT '图片附件列表',
    rating INT COMMENT '评价 1-5',
    tenant_id BIGINT COMMENT '租户ID',
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_ticket_no (ticket_no),
    INDEX idx_status (status),
    INDEX idx_creator (creator_id),
    INDEX idx_assignee (assignee_id),
    INDEX idx_tenant (tenant_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

### 3.2 工单处理记录表 (TicketProcessRecords)

```sql
CREATE TABLE ticket_process_records (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    ticket_id BIGINT NOT NULL,
    action VARCHAR(50) NOT NULL COMMENT '动作: dispatch/accept/reject/progress/finish/confirm/rate',
    operator_id BIGINT NOT NULL COMMENT '操作人ID',
    content TEXT COMMENT '操作说明/备注',
    from_status VARCHAR(20) COMMENT '操作前状态',
    to_status VARCHAR(20) NOT NULL COMMENT '操作后状态',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (ticket_id) REFERENCES tickets(id),
    INDEX idx_ticket (ticket_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

---

## 四、决策记录

| 日期 | 决策 | 理由 |
|------|------|------|
| 2026-05-06 | AuthService 专注认证，移除工单代码 | 职责单一，避免重复 |
| 2026-05-06 | 工单派单由 TicketService 调用 DispatchService | 解耦，DispatchService 专注派单规则 |
| 2026-05-06 | 工单编号格式: WO-YYYYMMDD-XXXX | 规范化，便于检索和统计 |
| 2026-05-06 | 使用 MySQL (wo_property) 统一存储 | 架构决策，所有服务统一数据库 |

---

**文档版本**：v1.0
**作者**：软件架构师
**审核**：软件负责人
**状态**：已确认
