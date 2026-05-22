# 工单模块字段定义

> **版本**：v1.1
> **模块**：ticket
> **最后更新**：2026-05-19
> **状态**：已确认（反映当前实际实现）

---

## 1. 字段分类表

**数据库表**: Tickets (TicketService)

| 序号 | 显示名 | 字段名 | 类型 | 分类 | 来源 | 必填 | 说明 |
|------|--------|--------|------|------|------|------|------|
| 1 | 工单编号 | ticketCode | text | 私有 | 后端自动生成 | 是 | 格式: REPAIR20260500001 |
| 2 | 工单标题 | title | text | 私有 | 前端/后端自动 | 是 | 创建时可自动以工单类型名称填充 |
| 3 | 工单类型 | ticketTypeId | select | 共享 | MasterDataService /api/ticket-types | 是 | 下拉选择，实际存储 TicketType(varchar) |
| 4 | 优先级 | priority | select | 共享 | 前端 hardcode | 是 | urgent/high/normal/low |
| 5 | 工单状态 | status | select | 共享 | 后端 | 是 | New/Pending/Dispatched/Accepted... |
| 6 | 工单描述 | description | textarea | 私有 | 前端 | 否 | |
| 7 | 区域 | areaId | select | 私有 | MasterDataService /api/hierarchy/areas | 否 | 三级联动第1级 |
| 8 | 楼栋 | buildingId | select | 私有 | MasterDataService /api/hierarchy/area-buildings | 否 | 三级联动第2级 |
| 9 | 房号 | roomId | select | 私有 | MasterDataService 级联加载 | 否 | 三级联动第3级 |
| 10 | 工种 | jobTypeIds | checkbox | 共享 | MasterDataService /api/job-types | 否 | 多选，按工单类型分组显示 |
| 11 | 联系人 | contactName | text | 共享 | 前端 | 否 | |
| 12 | 联系电话 | contactPhone | text | 共享 | 前端 | 否 | |
| 13 | 位置 | location | text | 共享 | 前端 | 否 | |
| 14 | 项目ID | projectId | int | 私有 | 前端固定传1 | 是 | 当前固定值 1 |

---

## 2. 前端表单字段（TicketList.vue 当前实现）

### 2.1 创建工单表单

```typescript
const form = ref({
  ticketTypeId: null as number | null,  // 工单类型
  title: '',                            // 自动以类型名称填充
  description: '',                      // 工单描述
  type: 'Repair',                       // 内部类型（保留）
  priority: 'Normal',                   // 优先级
  contactName: '',                      // 联系人
  contactPhone: '',                     // 联系电话
  locationId: null as number | null,   // 位置ID（保留）
  areaId: null as number | null,        // 区域ID
  buildingId: null as number | null,    // 楼栋ID
  roomId: null as number | null,        // 房号ID
  jobTypeIds: [] as number[]            // 工种多选
})
```

### 2.2 级联选择器

- **区域下拉**：`GET /api/hierarchy/areas` → `areas[]`
- **楼栋下拉**：`GET /api/hierarchy/area-buildings?areaId=X` → `buildings[]`
- **房号下拉**：从已加载楼栋数据的 `.rooms` 属性读取，不另发请求

### 2.3 工种多选（按工单类型分组）

```typescript
const groupedJobTypes = computed(() => {
  // 根据选中的 ticketTypeId，从 ticketTypes.jobTypes 中过滤
  // 返回 [{ name: '维修', items: [{id, name}, ...] }]
})
```

---

## 3. 后端接收字段（TenantCreateTicketRequest）

```csharp
public class TenantCreateTicketRequest
{
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string? Category { get; set; }
    public int? TicketTypeId { get; set; }    // 前端传来的工单类型ID
    public string? Priority { get; set; }       // "Normal" / "Medium" 等
    public string? Location { get; set; }
    public int ProjectId { get; set; }         // 固定传 1
    public List<string>? Images { get; set; }
}
```

**注意**：当前后端 `TenantCreateTicketRequest` **不接收** `areaId`、`buildingId`、`roomId`、`jobTypeIds`。这些字段前端暂存，但未提交到后端。

---

## 4. API 字段映射

| 前端字段 | 后端 TenantCreateTicketRequest | 数据库列 | 说明 |
|----------|-------------------------------|----------|------|
| ticketTypeId | TicketTypeId | ticket_types.id | 外键关联类型 |
| title | Title | Title | 直接映射 |
| description | Description | Description | 直接映射 |
| priority | Priority | Priority | varchar |
| projectId | ProjectId | project_id | INT |
| areaId | - (未传) | - | 前端暂存 |
| buildingId | - (未传) | - | 前端暂存 |
| roomId | - (未传) | - | 前端暂存 |
| jobTypeIds | - (未传) | - | 前端暂存 |

---

## 5. 共享字段来源

| 字段 | 来源服务 | API端点 | 说明 |
|------|---------|---------|------|
| 工单类型 | TicketService | GET /api/ticket-types | 内嵌 jobTypes |
| 工种 | MasterDataService | GET /api/job-types | 按 ticket_type_id 分组 |
| 区域 | MasterDataService | GET /api/hierarchy/areas | |
| 楼栋 | MasterDataService | GET /api/hierarchy/area-buildings?areaId=X | |
| 部门 | MasterDataService | GET /api/departments | 用于工种分类 |

---

## 6. 枚举值

### 6.1 工单状态

| code | name | 说明 |
|------|------|------|
| New | 新建 | 工单创建 |
| Pending | 待派单 | 等待调度 |
| Dispatched | 已派单 | 已指派处理人 |
| Accepted | 已接单 | 处理人已接单 |
| Rejected | 已拒单 | 处理人拒单 |
| Processing | 处理中 | 处理中 |
| Finished | 已完工 | 申请完工 |
| Confirmed | 已确认 | 确认完工 |
| Closed | 已关闭 | 完成 |

### 6.2 优先级

| code | name | 说明 |
|------|------|------|
| Urgent | 紧急 | 优先级 1 |
| High | 高 | 优先级 2 |
| Normal | 普通 | 优先级 3（默认值） |
| Low | 低 | 优先级 4 |

### 6.3 工单类型

| code | name | 说明 |
|------|------|------|
| REPAIR | 维修 | 默认类型 |
| SAFETY | 安保 | |
| CONSULT | 咨询 | |
| URGENT | 紧急 | |
| CLEANING | 清洁 | |
| COMPLAINT | 投诉 | |

---

**文档版本**：v1.1
**作者**：软件项目负责人
**审核**：软件架构师
**状态**：已确认（反映 2026-05-19 实际实现）