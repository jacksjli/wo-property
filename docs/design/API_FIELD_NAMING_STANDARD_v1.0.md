# API 字段命名规范 v1.0

> **目的**：统一前后端数据传输的字段命名格式，防止 PascalCase/camelCase 不匹配导致的 bug。
>
> **适用范围**：WO 物业管理软件的所有 REST API（前后端通信）。

---

## 1. 核心规范

### 1.1 强制使用 camelCase

所有 API 响应和请求中的字段名必须使用 **camelCase**（首字母小写，单词之间首字母大写）。

| ✅ 正确 | ❌ 错误 |
|---------|---------|
| `id`, `name`, `phoneNumber` | `Id`, `Name`, `PhoneNumber` |
| `employeeNo`, `departmentId` | `EmployeeNo`, `DepartmentId` |
| `isSupervisor`, `maxConcurrentTickets` | `IsSupervisor`, `MaxConcurrentTickets` |
| `ticketTypeIds`, `specialtyIds` | `TicketTypeIds`, `SpecialtyIds` |

### 1.2 数据库层映射

后端服务在数据库查询后，必须将字段名转换为 camelCase 再返回给前端。

```csharp
// ❌ 错误：直接返回数据库列名（PascalCase 或 snake_case）
return Ok(new { Id = 1, Name = "张三", CreatedAt = DateTime.Now });

// ✅ 正确：转换为 camelCase
var dict = new Dictionary<string, object>
{
    { "id", reader.GetInt32("Id") },
    { "name", reader.GetString("Name") },
    // ...
};
return Ok(new { success = true, data = dict });
```

### 1.3 数组字段（JSON 存储）

存储为 JSON 字符串的数组字段，键名统一使用 snake_case：

| 字段 | 数据库列名 | JSON 字符串内容 |
|------|-----------|-----------------|
| 工种 ID 列表 | `ticket_type_ids` | `[1, 2, 3]` |
| 专业技能 ID 列表 | `specialty_ids` | `[1, 5]` |
| 区域 ID 列表 | `area_ids` | `[1, 2, 3]` |
| 楼栋 ID 列表 | `building_ids` | `[1, 2]` |

### 1.4 时间字段

- 数据库列名：`created_at`, `updated_at`, `hire_date`
- API 响应：转为 camelCase → `createdAt`, `updatedAt`, `hireDate`
- 格式：`ISO 8601`（如 `2026-05-28T08:00:00`）

---

## 2. 服务端配置要求

### 2.1 ASP.NET Core 配置

所有服务必须配置 JSON 序列化选项：

```csharp
// Program.cs
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // 强制使用 camelCase
        options.JsonSerializerOptions.PropertyNamingPolicy =
            JsonNamingPolicy.CamelCase;
        // 字段名大小写不敏感
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
```

### 2.2 Dapper 查询结果转换

使用 Dapper 或原生 MySqlCommand 查询时，必须手动转换字段名：

```csharp
var row = new Dictionary<string, object>();
for (int i = 0; i < reader.FieldCount; i++)
{
    var colName = reader.GetName(i);  // 数据库列名
    var camelName = ToCamelCase(colName);  // 转为 camelCase
    row[camelName] = reader.GetValue(i) == DBNull.Value ? null : reader.GetValue(i);
}
items.Add(row);
```

### 2.3 工具函数

在 `Shared` 项目中提供转换工具：

```csharp
// WO.Shared/Utils/CaseConverter.cs
public static class CaseConverter
{
    public static string ToCamelCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return char.ToLowerInvariant(input[0]) + input[1..];
    }

    public static string ToSnakeCase(string input)
    {
        // implement conversion
    }
}
```

---

## 3. 前端接收规范

### 3.1 axios 配置

前端 axios 实例应配置大小写不敏感：

```typescript
// api/http.ts
const client = axios.create({
  baseURL,
  transformResponse: [(data) => {
    // axios 默认不转换，需要手动处理
    return JSON.parse(data);
  }]
});
```

### 3.2 TypeScript 接口定义

TypeScript 接口必须使用 camelCase：

```typescript
interface Personnel {
  id: number;
  name: string;
  employeeNo: string;
  phone: string;
  role: string;
  departmentId: number | null;
  ticketTypeIds: number[];
  isSupervisor: boolean;
}
```

### 3.3 数据映射函数

前端收到数据后，必须通过映射函数转换为 TypeScript 接口：

```typescript
const mapPersonnel = (p: any): Personnel => ({
  id: p.Id ?? p.id,
  name: p.Name ?? p.name,
  employeeNo: p.EmployeeNo ?? p.employeeNo,
  phone: p.Phone ?? p.phone,
  role: p.Role ?? p.role,
  departmentId: p.DepartmentId ?? p.departmentId ?? null,
  ticketTypeIds: p.TicketTypeIds
    ? JSON.parse(p.TicketTypeIds)
    : (p.ticket_type_ids ? JSON.parse(p.ticket_type_ids) : []),
  isSupervisor: p.IsSupervisor ?? p.is_supervisor ?? false,
});
```

**但更好的方式是：后端直接返回 camelCase，前端不需要每次映射。**

---

## 4. 规范检查清单

### 4.1 新增 API 时检查

- [ ] 控制器返回的 JSON 字段是否为 camelCase？
- [ ] 数据库列名到 API 字段的转换是否正确？
- [ ] 前端 TypeScript 接口字段名是否与 API 一致？
- [ ] 数组字段（JSON 字符串）的解析是否考虑多种格式？

### 4.2 代码审查检查

- [ ] 搜索 `Id`, `Name`, `Phone` 等 PascalCase 是否出现在 API 响应中
- [ ] 搜索 `reader.GetString("Id")` 等直接使用数据库列名的地方
- [ ] 检查前端是否有 `p.id` 但 API 返回的是 `p.Id`

---

## 5. 违反规范的处理

### 5.1 发现问题后

1. **立即修复**：修改后端代码，确保 API 返回 camelCase
2. **更新文档**：在 `docs/audit/PRODUCTION_*_AUDIT_{YYYY-MM}.md` 中记录
3. **通知前端**：确保前端知道字段名变化

### 5.2 临时兼容方案

如果无法立即修改后端，前端可以使用兼容模式：

```typescript
// 临时兼容 PascalCase 和 camelCase
const getField = (obj: any, ...candidates: string[]) => {
  for (const key of candidates) {
    if (obj[key] !== undefined) return obj[key];
  }
  return undefined;
};

// 使用
const id = getField(p, 'id', 'Id');
const name = getField(p, 'name', 'Name');
```

---

## 6. 规范版本历史

| 版本 | 日期 | 修改内容 |
|------|------|---------|
| v1.0 | 2026-05-28 | 初始版本，规定 camelCase 为唯一标准 |

---

_本规范由芦苇创建，2026-05-28_
_原因：发现 PersonService 返回 PascalCase 导致前端 row.id 为 undefined_