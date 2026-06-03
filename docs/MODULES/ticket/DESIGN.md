# 工单模块设计

> **版本**：v1.1
> **服务**：TicketService (5102)
> **状态**：已确认（更新多项目支持）
> **更新日期**：2026-06-01

---

## 一、概述

工单模块是 WO 物业管理软件的核心模块，负责物业维修、巡检、安保等各类工单的**全生命周期管理**。

### 1.1 核心职责

```
TicketService (5102) 职责：
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
  - DispatchService (5241)：派单服务
```

### 1.3 多项目支持

```
工单模块支持多小区（多项目）并行管理：
  - 每个项目有独立的工单编号序列（按项目 + 月独立重置）
  - 通过 HTTP Header X-Project-Code 传递项目上下文
  - API 层按项目代码过滤数据，实现项目间数据隔离
```

---

## 二、工单编号规范

### 2.1 格式定义（多项目版本）

```
格式: {ProjectCode}-WO-YYYYMM-NNNNN
示例: YGHY001-WO-202606-10001
     YGXY001-WO-202606-10001

说明:
  - ProjectCode: 项目代码（3-10位，如 YGHY001、YGXY001）
  - WO: 工单类型前缀，固定
  - YYYYMM: 6位年月（2026年6月 = 202606）
  - NNNNN: 5位序号，从10001开始，不足5位不补零
  - 各项目独立序列，互不干扰
```

### 2.2 生成规则

```
1. 同一项目同一月内的工单编号连续递增
2. 各项目每月从10001开始，独立重置
3. 5位序号用尽后自动扩展为6位（100001起）
4. 新的一月（跨年跨月）序号位数重置回5位
5. 编号在创建时自动生成，不允许手动修改
6. 项目代码从 X-Project-Code HTTP Header 获取
```

### 2.3 流转示例（双项目并行）

```
阳光花园（YGHY001）2026年6月：
  YGHY001-WO-202606-10001
  YGHY001-WO-202606-10002
  ...

阳光新苑（YGXY001）2026年6月：
  YGXY001-WO-202606-10001    ← 与 YGHY001 独立，序号各自从10001开始
  YGXY001-WO-202606-10002
  ...

跨月重置示例：
  2026年5月：YGHY001-WO-202605-10001 ~ YGHY001-WO-202605-99999
  2026年6月：YGHY001-WO-202606-10001    ← 重置回5位，序号从10001开始
```

---

## 三、数据库模型

### 3.1 工单主表 (Tickets)

```sql
CREATE TABLE tickets (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    ticket_code VARCHAR(50) NOT NULL UNIQUE COMMENT '工单编号 {ProjectCode}-WO-YYYYMM-NNNNN',
    title VARCHAR(200) NOT NULL COMMENT '工单标题：ticketTypeName + jobTypeName',
    description TEXT COMMENT '工单描述',
    category VARCHAR(50) COMMENT '工单分类',
    priority VARCHAR(20) DEFAULT 'Medium' COMMENT 'Low/Normal/Medium/High/Urgent',
    status VARCHAR(20) NOT NULL DEFAULT 'New' COMMENT '状态',
    dispatch_status VARCHAR(50) COMMENT '派工状态：Pending/Assigned/Accepted/Rejected',
    creator_person_id BIGINT COMMENT '创建人ID',
    assignee_person_id BIGINT COMMENT '指派人ID',
    project_code VARCHAR(20) DEFAULT 'YGHY001' COMMENT '项目代码',
    location VARCHAR(500) COMMENT '位置描述',
    area_id BIGINT COMMENT '区域ID',
    building_id BIGINT COMMENT '楼栋ID',
    room VARCHAR(50) COMMENT '房间号',
    images JSON COMMENT '图片附件列表',
    contact_person_name VARCHAR(100) COMMENT '联系人姓名',
    contact_phone VARCHAR(20) COMMENT '联系人电话',
    job_type_id INT COMMENT '工种ID',
    rating INT COMMENT '评价 1-5',
    tenant_id BIGINT DEFAULT 1 COMMENT '租户ID',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_ticket_code (ticket_code),
    INDEX idx_status (status),
    INDEX idx_project_code (project_code),
    INDEX idx_creator (creator_person_id),
    INDEX idx_assignee (assignee_person_id)
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

## 四、API 契约（多项目版本）

### 4.1 项目上下文传递

```
所有工单相关 API 必须在 Header 中传递项目代码：
  X-Project-Code: YGHY001   ← 必填，标识当前操作的项目

响应示例：
  {
    "success": true,
    "data": [...],
    "total": 20,
    "page": 1,
    "pageSize": 20
  }
```

### 4.2 项目隔离规则

```
1. 查询：仅返回 X-Project-Code 指定项目的工单
2. 创建：工单编号前缀使用 X-Project-Code 的值
3. 列表：按项目 + 月分组，工单编号格式 {ProjectCode}-WO-YYYYMM-NNNNN
4. 统计：按项目独立统计，不跨项目汇总
```

---

## 五、决策记录

| 日期 | 决策 | 理由 |
|------|------|------|
| 2026-05-06 | AuthService 专注认证，移除工单代码 | 职责单一，避免重复 |
| 2026-05-06 | 工单派单由 TicketService 调用 DispatchService | 解耦，DispatchService 专注派单规则 |
| 2026-05-06 | 使用 MySQL (wo_property) 统一存储 | 架构决策，所有服务统一数据库 |
| 2026-06-01 | 工单编号增加项目前缀 {ProjectCode}-WO-YYYYMM-NNNNN | 多项目独立编号，避免冲突 |
| 2026-06-01 | 各项目工单序列独立，按项目+月重置 | 每个小区物业独立运营，编号互不干扰 |
| 2026-06-01 | X-Project-Code HTTP Header 传递项目上下文 | API 层统一入口，无需每个请求参数都带项目信息 |

---

## 六、多项目支持说明

### 6.1 前端交互流程

```
1. 登录 → 返回用户归属的 projects 数组（含 projectCode/projectName）
2. 首页底部 ProjectTabBar → 显示项目切换栏
3. 切换项目 → 存储 currentProject，刷新工单列表
4. 创建/查询工单 → HTTP Header X-Project-Code 自动附加
```

### 6.2 后端数据隔离

```
TenantTicketController.GetTickets:
  - 读取 X-Project-Code header
  - WHERE project_code = :projectCode

TenantTicketController.CreateTicket:
  - 读取 X-Project-Code header
  - 工单编号 = {ProjectCode}-WO-{YYYYMM}-{NNNNN}
  - project_code 字段写入数据库
```

---

**文档版本**：v1.1
**更新内容**：多项目编号规范、多项目 API 契约、多项目数据隔离说明
**作者**：软件架构师
**审核**：软件负责人（🪽的芦苇）
**状态**：已确认