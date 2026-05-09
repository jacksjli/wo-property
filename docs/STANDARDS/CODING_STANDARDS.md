# 代码编写规范

> **版本**：v1.0
> **最后更新**：2026-05-05

---

## 1. 概述

本规范定义了 WO 物业管理软件的代码编写标准，确保代码风格一致、易于维护。

---

## 2. C# 后端规范

### 2.1 命名规范

| 类型 | 规范 | 示例 |
|------|------|------|
| 类名 | PascalCase | `TicketService`, `PersonController` |
| 方法名 | PascalCase | `GetTicketById`, `CreateTicket` |
| 参数名 | camelCase | `ticketId`, `pageNumber` |
| 私有字段 | _camelCase | `_ticketRepository`, `_httpClient` |
| 常量 | PascalCase | `MaxPageSize`, `DefaultPageNumber` |
| 接口名 | I + PascalCase | `ITicketRepository`, `IServiceClient` |

### 2.2 方法规范

```csharp
// ✅ 正确：方法单一职责，参数清晰
public async Task<TicketDto> CreateTicketAsync(CreateTicketRequest request, CancellationToken ct)
{
    // 验证
    if (request == null)
        throw new ArgumentNullException(nameof(request));
    
    // 业务逻辑
    var ticket = await _ticketRepository.CreateAsync(request, ct);
    
    // 返回
    return _mapper.Map<TicketDto>(ticket);
}

// ❌ 错误：方法过长，职责不清
public async Task DoEverythingAsync(...) { /* 500行代码 */ }
```

### 2.3 异步方法

```csharp
// ✅ 正确：使用 Async 后缀，返回 Task
public async Task<TicketDto> GetTicketByIdAsync(int id, CancellationToken ct = default)
{
    var ticket = await _ticketRepository.GetByIdAsync(id, ct);
    return _mapper.Map<TicketDto>(ticket);
}

// ❌ 错误：同步方法伪装成异步
public TicketDto GetTicketById(int id) { /* ... */ }
```

### 2.4 异常处理

```csharp
// ✅ 正确：业务异常使用 Result 模式或抛出特定异常
public async Task<Result<TicketDto>> GetTicketAsync(int id)
{
    var ticket = await _ticketRepository.GetByIdAsync(id);
    if (ticket == null)
        return Result.Failure<TicketDto>("Ticket not found");
    
    return Result.Success(_mapper.Map<TicketDto>(ticket));
}

// ❌ 错误：所有错误都抛异常
public async Task<TicketDto> GetTicketAsync(int id)
{
    var ticket = await _ticketRepository.GetByIdAsync(id);
    if (ticket == null)
        throw new NotFoundException("Ticket not found");
}
```

---

## 3. Vue3 前端规范

### 3.1 组件命名

```vue
<!-- ✅ 正确：使用 PascalCase 或 kebab-case -->
<template>
  <TicketList />
  <ticket-list />
  <TicketForm />
</template>

<!-- ❌ 错误：混用大小写 -->
<template>
  <ticketList />  <!-- 不一致 -->
</template>
```

### 3.2 组件结构

```vue
<template>
  <div class="ticket-form">
    <!-- 模板部分 -->
  </div>
</template>

<script setup lang="ts">
// ✅ 正确：使用 <script setup>，按顺序组织
// 1. 导入
import { ref, computed, onMounted } from 'vue'
import { useTicketStore } from '@/stores/ticket'
import type { Ticket, CreateTicketRequest } from '@/types'

// 2. Props / Emits
const props = defineProps<{ ticketId?: number }>()
const emit = defineEmits<{ (e: 'saved', ticket: Ticket): void }>()

// 3. Composables

// 4. Reactive state
const form = ref<CreateTicketRequest>({ title: '', description: '' })

// 5. Computed

// 6. Methods

// 7. Lifecycle
onMounted(() => { /* ... */ })
</script>

<style scoped>
/* ✅ 正确：scoped 样式 */
.ticket-form {
  padding: 16px;
}
</style>
```

### 3.3 API 调用

```typescript
// ✅ 正确：统一使用 apiClient 封装
import { ticketApi } from '@/api/ticket'

export const ticketApi = {
  getList(params: TicketQueryParams) {
    return apiClient.get<PaginatedResult<Ticket>>('/tickets', { params })
  },
  
  create(data: CreateTicketRequest) {
    return apiClient.post<Ticket>('/tickets', data)
  },
}

// ❌ 错误：直接使用 axios
export const getTicket = (id: number) => axios.get(`/api/tickets/${id}`)
```

---

## 4. 数据库规范

### 4.1 表名命名

```
✓ 正确：复数名词，小写，下划线分隔
  tickets, ticket_process_records, persons

✗ 错误：混合大小写或驼峰
  Ticket, TicketProcessRecords, ticketRecords
```

### 4.2 字段命名

```
✓ 正确：小写，下划线分隔
  ticket_no, created_at, assignee_id

✗ 错误：驼峰或匈牙利命名
  ticketNo, createdAt, iAssigneeId
```

### 4.3 索引命名

```
✓ 正确：idx_表名_字段名
  idx_tickets_status, idx_tickets_assignee

✗ 错误：随意命名
  status_idx, index1
```

---

## 5. Git 提交规范

```
格式：<type>(<scope>): <subject>

示例：
  feat(ticket): 新增工单派单功能
  fix(ticket): 修复工单状态流转问题
  docs(api): 更新 API 文档
  refactor(person): 重构人员服务代码
  test(device): 添加设备模块测试用例

type:
  feat: 新功能
  fix: 修复 bug
  docs: 文档更新
  style: 代码格式（不影响功能）
  refactor: 重构
  test: 测试
  chore: 构建/工具
```

---

**文档版本**：v1.0
**作者**：软件架构师
**审核**：软件负责人
**状态**：待评审
