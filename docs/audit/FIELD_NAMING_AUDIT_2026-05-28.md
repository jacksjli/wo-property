# API 字段命名问题检查报告

> **时间**: 2026-05-28 08:06
> **检查范围**: 所有后端服务 (16个)
> **目的**: 全面检查字段命名规范执行情况，提供修复方案

---

## 一、检查结果汇总

### 1.1 服务状态一览

| 服务 | 端口 | 运行中 | JSON配置 | 返回格式问题 |
|------|------|--------|----------|--------------|
| **PersonService** | 5018 | ✅ | ✅ 已配置 | ❌ **PascalCase (Dictionary)** |
| **TicketService** | 5102 | ✅ | ✅ 已配置 | ✅ camelCase |
| **MasterDataService** | 5019 | ✅ | ✅ 已配置 | ✅? |
| **AuthService** | 5106 | ✅ | ✅ 已配置 | ✅? |
| **MaterialService** | 5504 | ❌ | ❌ **未配置** | ⚠️ 需测试 |
| **DeviceService** | 5530 | ❌ | ❌ **未配置** | ⚠️ 需测试 |
| **DispatchService** | 5241 | ❌ | ❌ **未配置** | ⚠️ 需测试 |
| AnnouncementService | 5511 | ❌ | ✅ 已配置 | ⚠️ 需测试 |
| ContractService | 5501 | ❌ | ✅ 已配置 | ⚠️ 需测试 |
| KeyService | 5512 | ❌ | ✅ 已配置 | ⚠️ 需测试 |
| MobileService | 5526 | ❌ | ✅ 已配置 | ⚠️ 需测试 |
| ParkingService | 5525 | ❌ | ✅ 已配置 | ⚠️ 需测试 |
| PaymentService | 5507 | ❌ | ✅ 已配置 | ⚠️ 需测试 |
| RenovationService | 5521 | ❌ | ✅ 已配置 | ⚠️ 需测试 |

### 1.2 已验证的字段格式

| 服务 | API | 字段格式 |
|------|-----|----------|
| PersonService | `/api/persons` | ❌ `Id`, `Name`, `Phone`, `Role` (PascalCase) |
| TicketService | `/api/tenant/tickets` | ✅ `id`, `ticketCode`, `title`, `status` (camelCase) |

---

## 二、问题分类

### 第一类：Dictionary 直接返回数据库列名（最严重）

**问题服务**: PersonService

**原因分析**:
```csharp
// PersonsController.cs GetPersons 方法
var items = new List<Dictionary<string, object>>();
while (await reader.ReadAsync())
{
    var row = new Dictionary<string, object>();
    for (int i = 0; i < reader.FieldCount; i++)
    {
        var val = reader.GetValue(i);
        row[reader.GetName(i)] = val == DBNull.Value ? null : val;  // 直接使用数据库列名
    }
    items.Add(row);
}
// 返回: { Id, Name, Phone, EmployeeNo } - PascalCase
```

**影响**:
- API 响应包含 `Id`, `Name`, `Phone` 等 PascalCase 字段
- 前端 `p.id`, `p.name`, `p.phone` 全为 `undefined`
- 任何依赖这些字段的功能（删除、编辑、详情）全部失效

**说明**: 即使配置了 `JsonNamingPolicy.CamelCase`，Dictionary 的键名由数据库列名决定，不受 JSON 配置影响。

---

### 第二类：缺少 JSON 序列化配置

**问题服务**: MaterialService, DeviceService, DispatchService

**原因分析**:
```csharp
// MaterialService/Program.cs
builder.Services.AddControllers();
// 缺少 AddJsonOptions 配置
```

**代码示例**:
```csharp
// MaterialService 返回
new {
    id = m.Id,        // C# 属性名 = PascalCase
    name = m.Name,    // 即使 m 是 CamelCase，匿名对象属性名由编译器决定
}
```

**问题**:
- C# 匿名对象的属性名由编译器根据赋值右侧的属性名决定
- `new { id = m.Id }` 的属性名是 `id`，但 `new { Id = m.Id }` 的属性名是 `Id`
- 如果代码写 `id = m.Id`，返回 `{ id: 1 }` - 正确
- 如果代码写 `Id = m.Id`，返回 `{ Id: 1 }` - 错误

**需要检查各服务的 Controller 代码确认实际写法。**

---

### 第三类：其他服务（有配置但需验证）

以下服务配置了 CamelCase，但匿名对象的属性名需要确认：
- AnnouncementService
- ContractService
- KeyService
- MobileService
- ParkingService
- PaymentService
- RenovationService

---

## 三、问题严重程度评级

| 优先级 | 服务 | 问题类型 | 风险 |
|--------|------|----------|------|
| **P0** | PersonService | Dictionary 返回 PascalCase | 高 - 已影响删除功能 |
| **P1** | MaterialService | 缺少 JSON 配置 | 高 - 前端会崩溃 |
| **P1** | DeviceService | 缺少 JSON 配置 | 高 |
| **P1** | DispatchService | 缺少 JSON 配置 | 高 |
| **P2** | 其他 7 个服务 | 需启动验证 | 中 |

---

## 四、修复方案

### 方案一：后端修复（推荐，永久解决）

#### 4.1 PersonService 修复

**问题根因**: `GetPersons` 使用 Dictionary 直接返回数据库列名

**修复方式**: 将 Dictionary 的键名转为 camelCase

```csharp
// 修改 PersonsController.cs
var items = new List<Dictionary<string, object>>();
while (await reader.ReadAsync())
{
    var row = new Dictionary<string, object>();
    for (int i = 0; i < reader.FieldCount; i++)
    {
        var val = reader.GetValue(i);
        if (val != DBNull.Value)
        {
            var colName = reader.GetName(i);
            var camelName = ToCamelCase(colName);  // 转换列名
            row[camelName] = val;
        }
    }
    items.Add(row);
}

// 添加工具函数
private static string ToCamelCase(string str)
{
    if (string.IsNullOrEmpty(str)) return str;
    return char.ToLowerInvariant(str[0]) + str[1..];
}
```

#### 4.2 添加 JSON 配置（缺少的服务）

**MaterialService/Program.cs**:
```csharp
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
```

**DeviceService/Program.cs**: 同上

**DispatchService/Program.cs**: 同上

#### 4.3 检查匿名对象写法

对于已配置 CamelCase 的服务，检查 Controller 中的返回对象：

| 正确写法 | 错误写法 |
|----------|----------|
| `new { id = m.Id }` | `new { Id = m.Id }` |
| `new { name = m.Name }` | `new { Name = m.Name }` |
| `new { createdAt = m.CreatedAt }` | `new { CreatedAt = m.CreatedAt }` |

---

### 方案二：前端兼容（临时，不推荐用于新功能）

前端添加字段映射函数：

```typescript
// utils/fieldMapper.ts
export const mapToCamelCase = (obj: any): any => {
  if (Array.isArray(obj)) {
    return obj.map(mapToCamelCase);
  }
  if (obj !== null && typeof obj === 'object') {
    return Object.keys(obj).reduce((acc, key) => {
      const camelKey = key.replace(/_([a-z])/g, (_, c) => c.toUpperCase());
      acc[camelKey] = mapToCamelCase(obj[key]);
      return acc;
    }, {} as any);
  }
  return obj;
};
```

**缺点**:
- 治标不治本
- 每个前端页面都要添加映射
- 性能损耗
- 未来维护困难

---

## 五、推荐修复步骤

### 阶段一：修复 P0 问题（立即）

1. **PersonService**
   - 修改 `GetPersons` 方法，将 Dictionary 键名转为 camelCase
   - 添加 `ToCamelCase` 工具函数
   - 测试验证

### 阶段二：修复 P1 问题（本周）

2. **MaterialService / DeviceService / DispatchService**
   - 添加 JSON 配置
   - 检查 Controller 返回对象
   - 统一字段名为 camelCase

### 阶段三：验证 P2 服务（下周）

3. **启动其他 7 个服务**
   - 逐一测试 API 响应
   - 如有问题，参照阶段二修复

### 阶段四：前端去兼容化（持续）

4. **移除临时兼容代码**
   - PersonnelList.vue 中的 `p.Id ?? p.id` 等兼容写法
   - 确保后端返回正确的 camelCase

---

## 六、验证方法

### 后端验证

```bash
# 检查 PersonService
curl -s "http://localhost:5018/api/persons?pageSize=1" | python3 -c "
import sys, json
d = json.load(sys.stdin)
item = d['data']['items'][0]
print('Keys:', list(item.keys())[:5])
# 应该是 ['id', 'name', 'phone'] 而不是 ['Id', 'Name', 'Phone']
"
```

### 前端验证

打开浏览器 DevTools，Network 标签，查看 API 响应：
- ✅ 正确: `{ id: 1, name: "张三", phone: "13800138000" }`
- ❌ 错误: `{ Id: 1, Name: "张三", Phone: "13800138000" }`

---

## 七、后续预防措施

1. **代码审查**
   - 规定：所有 Controller 返回必须使用 camelCase
   - 审查清单增加字段名检查

2. **自动化测试**
   - 添加 API 响应字段格式测试
   - 断言所有字段名为 camelCase

3. **文档更新**
   - 在 `API_FIELD_NAMING_STANDARD_v1.0.md` 中增加 Dictionary 返回的注意事项

---

_报告生成时间: 2026-05-28 08:06_
_芦苇_
---

## 八、修复执行记录 (2026-05-28 08:14)

### 8.1 PersonService 修复 (P0)

**问题**: GetPersons 等方法使用 Dictionary 直接返回数据库列名 (PascalCase)

**修复内容**:
1. 添加 `ToCamelCase()` 工具方法 (line 18-37)
2. 修改所有 Dictionary 返回逻辑，将列名转为 camelCase
3. 影响方法:
   - `GetPersons()` - line ~104
   - `GetPerson()` - line ~144
   - `GetByRole()` - line ~323
   - `GetByDepartment()` - line ~352
   - `GetStatistics()` - line ~379

**验证结果**:
```bash
curl http://localhost:5018/api/persons?pageSize=1
# 返回: {"id":40, "employeeNo":"EMP15152", "name":"张维修", ...} ✅
```

### 8.2 MaterialService 修复 (P1)

**问题**: 缺少 JSON 序列化配置

**修复内容**:
1. 添加 `using System.Text.Json;`
2. 添加 `using System.Text.Json.Serialization;`
3. 将 `ServiceRunner.ConfigurePort()` 替换为 `builder.WebHost.UseUrls()`
4. 添加 `.AddJsonOptions()` 配置

**编译结果**: ✅ 成功

### 8.3 DeviceService 修复 (P1)

**问题**: 缺少 JSON 序列化配置

**修复内容**:
1. 添加 `using System.Text.Json;`
2. 添加 `using System.Text.Json.Serialization;`
3. 将 `ServiceRunner.ConfigurePort()` 替换为 `builder.WebHost.UseUrls()`
4. 添加 `.AddJsonOptions()` 配置

**编译结果**: ✅ 成功

### 8.4 DispatchService 修复 (P1)

**问题**: 缺少 JSON 序列化配置

**修复内容**:
1. 添加 `.AddJsonOptions()` 配置 (已有 UseUrls 配置)

**编译结果**: ✅ 成功

---

## 九、后续工作

| 工作项 | 状态 | 说明 |
|--------|------|------|
| PersonService 修复验证 | ✅ 完成 | API 返回 camelCase |
| MaterialService JSON 配置 | ✅ 完成 | 已验证 id, materialNo, name... |
| DeviceService JSON 配置 | ✅ 完成 | 已验证 id, code, name... |
| DispatchService JSON 配置 | ✅ 完成 | 正常 |
| 前端临时兼容代码清理 | ✅ 完成 | PersonnelList.vue 已清理 |
| 其他服务验证 (7个) | ⚠️ 部分完成 | 需修复 TenantDbFactory |

### 9.1 任务1-3 完成情况

**任务1: 启动三个服务验证** ✅
- MaterialService (5504): ✅ 返回 `id, materialNo, name, category...`
- DeviceService (5530): ✅ 返回 `id, code, name, deviceTypeId...`
- DispatchService (5241): ✅ 正常运行

**任务2: 清理前端临时兼容代码** ✅
- PersonnelList.vue: 移除所有 `p.Id ?? p.id` 映射
- 现在直接使用 camelCase 字段

**任务3: 验证其他7个服务** ⚠️ 部分完成
- AnnouncementService (5511): 数据库路由问题
- ContractService (5501): Tenant code not set
- KeyService (5512): Tenant code not set
- MobileService (5526): 需进一步调试
- ParkingService (5525): 需进一步调试
- PaymentService (5507): 需进一步调试
- RenovationService (5521): 需进一步调试

**注**: 7个服务都有 TenantDbFactory 配置问题，需要修复数据库路由才能完整验证。核心的4个服务 (PersonService, MaterialService, DeviceService, DispatchService) 已验证通过。

