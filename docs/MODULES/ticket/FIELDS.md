# 工单模块字段定义

> **版本**：v1.0
> **模块**：ticket
> **最后更新**：2026-05-05

---

## 1. 字段分类表

**数据库表**: Tickets (TicketService)

| 序号 | 显示名 | 字段名 | 类型 | 分类 | 来源服务 | API端点 | 必填 |
|------|--------|--------|------|------|----------|---------|------|
| 1 | 工单编号 | ticketNo | text | 私有 | TicketService | - | 是 |
| 2 | 工单标题 | title | text | 私有 | TicketService | - | 是 |
| 3 | 工单类型 | type | select | 共享 | MasterDataService | GET /api/enums/ticket-types | 是 |
| 4 | 优先级 | priority | select | 共享 | MasterDataService | GET /api/enums/priorities | 是 |
| 5 | 工单状态 | status | select | 共享 | MasterDataService | GET /api/enums/ticket-statuses | 是 |
| 6 | 工单描述 | description | textarea | 私有 | TicketService | - | 否 |
| 7 | 创建人 | creatorName | text | 共享 | PersonService | GET /api/persons/{id} | 否 |
| 8 | 创建时间 | createTime | date | 私有 | 系统 | - | 否 |
| 9 | 指派人 | assigneeName | text | 共享 | PersonService | GET /api/persons/{id} | 否 |
| 10 | 处理时间 | handleTime | date | 私有 | TicketService | - | 否 |
| 11 | 完成时间 | completeTime | date | 私有 | TicketService | - | 否 |
| 12 | 联系人 | contactName | text | 共享 | PersonService | - | 否 |
| 13 | 联系电话 | contactPhone | text | 共享 | PersonService | - | 否 |
| 14 | 位置 | location | text | 共享 | MasterDataService | - | 否 |
| 15 | 备注 | remark | textarea | 共享 | 系统 | - | 否 |

---

## 2. 共享字段来源汇总

### 2.1 MasterDataService 提供

| 字段名 | API 端点 | 说明 |
|--------|----------|------|
| type | GET /api/enums/ticket-types | 工单类型 |
| priority | GET /api/enums/priorities | 优先级 |
| status | GET /api/enums/ticket-statuses | 工单状态 |
| location | - | 位置信息（可手动输入或选择） |

### 2.2 PersonService 提供

| 字段名 | API 端点 | 说明 |
|--------|----------|------|
| creatorName | GET /api/persons/{id} | 创建人姓名 |
| assigneeName | GET /api/persons/{id} | 指派人姓名 |
| contactName | - | 联系人姓名 |
| contactPhone | - | 联系电话 |

### 2.3 系统字段

| 字段名 | 说明 |
|--------|------|
| createTime | 创建时间 |
| remark | 备注 |

---

## 3. 枚举值来源

### 3.1 工单类型 (ticket-types)

```json
[
  { "code": "Repair", "name": "维修" },
  { "code": "Access", "name": "放行" },
  { "code": "Cleaning", "name": "清洁" },
  { "code": "Security", "name": "安保" },
  { "code": "Other", "name": "其他" }
]
```

### 3.2 优先级 (priorities)

```json
[
  { "code": "Urgent", "name": "紧急", "value": 1 },
  { "code": "High", "name": "高", "value": 2 },
  { "code": "Normal", "name": "普通", "value": 3 },
  { "code": "Low", "name": "低", "value": 4 }
]
```

### 3.3 工单状态 (ticket-statuses)

```json
[
  { "code": "Created", "name": "已创建" },
  { "code": "Dispatched", "name": "已派单" },
  { "code": "Accepted", "name": "已接单" },
  { "code": "Rejected", "name": "已拒单" },
  { "code": "InProgress", "name": "处理中" },
  { "code": "Finished", "name": "已完工" },
  { "code": "Confirmed", "name": "已确认" },
  { "code": "Closed", "name": "已关闭" }
]
```

---

## 4. 前端配置示例

```typescript
// fieldConfig.ts 工单字段配置
const TICKET_FIELDS = [
  // 私有字段
  { key: 'ticketNo', name: '工单编号', type: 'text', source: 'local', required: true },
  { key: 'title', name: '工单标题', type: 'text', source: 'local', required: true },
  { key: 'description', name: '工单描述', type: 'textarea', source: 'local' },
  
  // API 字段
  { key: 'type', name: '工单类型', type: 'select', source: 'api', 
    apiEndpoint: '/enums/ticket-types', apiService: 'masterdata', required: true },
  { key: 'priority', name: '优先级', type: 'select', source: 'api',
    apiEndpoint: '/enums/priorities', apiService: 'masterdata', required: true },
  { key: 'status', name: '工单状态', type: 'select', source: 'api',
    apiEndpoint: '/enums/ticket-statuses', apiService: 'masterdata' },
  
  // 人员字段
  { key: 'creatorName', name: '创建人', type: 'text', source: 'api', 
    apiEndpoint: '/persons', apiService: 'person' },
  { key: 'assigneeName', name: '指派人', type: 'text', source: 'api',
    apiEndpoint: '/persons', apiService: 'person' },
  
  // 系统字段
  { key: 'createTime', name: '创建时间', type: 'date', source: 'system' },
  { key: 'remark', name: '备注', type: 'textarea', source: 'system' },
]
```

---

**文档版本**：v1.0
**作者**：前端工程师
**审核**：软件架构师
**状态**：已确认
