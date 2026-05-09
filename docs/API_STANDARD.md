# WO-Property API 标准化规范

## 一、API 架构原则

### 单一数据流
```
前端 → masterApi → /api/master-data/{resource} → MasterDataService → MySQL (wo_property)
```

### 两条硬规则
1. **所有业务数据必须写入 MySQL**，禁止 localStorage 作为主数据源
2. **所有前端 API 调用必须通过 masterApi**，禁止硬编码端口

---

## 二、API 路径映射表

| 前端路由 | API 路径 | 后端 Controller | MySQL 表 | 状态 |
|----------|----------|----------------|----------|------|
| /finance | /api/finance-records | FinanceRecordsController | FinanceRecords | ✅ |
| /cleaning | /api/cleaning-records | CleaningRecordsController | CleaningRecords | ✅ |
| /community | /api/community-activities | CommunityActivitiesController | CommunityActivities | ✅ |
| /renovation | /api/renovation-requests | RenovationController | RenovationRequests | ✅ |
| /express | /api/express-records | ExpressRecordsController | ExpressRecords | ✅ |
| /delivery | /api/delivery-requests | DeliveryRequestsController | DeliveryRequests | ✅ |
| /device | /api/devices | DevicesController | Devices | 🔄 重构中 |
| /contract | /api/contracts | ContractsController | Contracts | 🔄 重构中 |
| /material | /api/materials | MaterialsController | Materials | 🔄 重构中 |
| /complaint | /api/complaints | ComplaintsController | Complaints | 🔄 重构中 |
| /inspection | /api/inspections | InspectionsController | Inspections | 🔄 重构中 |
| /key | /api/keys | KeysController | Keys | 🔄 重构中 |
| /parking | /api/parking-records | ParkingRecordsController | ParkingRecords | 🔄 重构中 |
| /payment | /api/payments | PaymentsController | Payments | 🔄 重构中 |
| /visitor | /api/visitors | VisitorsController | Visitors | 🔄 重构中 |
| /resident | /api/residents | ResidentsController | Residents | 🔄 重构中 |
| /personnel | /api/personnel | PersonnelController | Personnel | 🔄 重构中 |
| /takeout | /api/takeout-orders | TakeoutOrdersController | TakeoutOrders | 🔄 重构中 |
| /tickets | /api/tickets | TicketController | Tickets | ✅ (TicketService:5002) |
| /statistics | /api/statistics | StatisticsController | - | ✅ (StatisticsService:5014) |
| /master/* | /api/{resource} | 各 Controller | 各表 | ✅ |

---

## 三、Gateway 路由配置（一次性配置）

**文件**：`src/WO.Property.APIGateway/appsettings.json`

```json
{
  "ReverseProxy": {
    "Routes": {
      "master-data": {
        "ClusterId": "master-data-service",
        "Match": { "Path": "/api/master-data/{**catch-all}" },
        "Transforms": [{ "PathPattern": "/api/{**catch-all}" }]
      }
    },
    "Clusters": {
      "master-data-service": {
        "Destinations": {
          "master-data1": { "Address": "http://localhost:5019" }
        }
      }
    }
  }
}
```

**注意**：新增模块无需修改 Gateway，只要前端用 `masterApi` 请求，catch-all 自动路由。

---

## 四、前端 API 客户端规范

### 正确用法
```typescript
import { masterApi } from '@/api/http'

// GET
const res: any = await masterApi.get('/finance-records', { params: { page: 1, pageSize: 20 } })
list.value = res.data || []

// POST
await masterApi.post('/finance-records', payload)

// PUT
await masterApi.put(`/finance-records/${id}`, payload)

// DELETE
await masterApi.delete(`/finance-records/${id}`)
```

### 禁止用法
```typescript
// ❌ 禁止硬编码端口
const res = await axios.get('http://localhost:5019/api/xxx')

// ❌ 禁止 localStorage store 作为数据源
const list = ref(getAllDevices())  // 这来自 stores/device.ts (localStorage)

// ❌ 禁止绕过 masterApi
import { someOtherApi } from '@/api/other'
```

---

## 五、后端 Controller 规范

### 标准结构（参考 FinanceRecordsController.cs）
```csharp
[ApiController]
[Route("api/xxx-records")]  // 路径必须与 API 路径映射表一致
public class XxxRecordsController : ControllerBase
{
    private readonly MySqlConnection _db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        // 分页查询
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id) { ... }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] XxxRecordDto dto) { ... }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] XxxRecordDto dto) { ... }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id) { ... }
}
```

---

## 六、数据库表命名规范

```
{resource_name}_records    // 多个记录：cleaning_records, finance_records
{resource_name}s          // 实体集合：devices, contracts, materials
activities               // 活动类：community_activities
requests                 // 请求类：renovation_requests, delivery_requests
```

---

## 七、新模块创建 Checklist

- [ ] 创建 MySQL 表（参考 wo_property 数据库）
- [ ] 在 MasterDataService/Controllers/ 创建 Controller
- [ ] 前端 views/{module}/*.vue 使用 `masterApi.get('/xxx')`
- [ ] 确保无 localStorage store 调用（除了 fieldConfig）
- [ ] 通过 curl 验证 API 路由
- [ ] 更新本文档 API 路径映射表

---

## 八、已验证 API 列表（定期更新）

| API | 验证时间 | 状态 |
|-----|----------|------|
| /api/finance-records | 2026-05-09 | ✅ 200 |
| /api/cleaning-records | 2026-05-09 | ✅ 200 |
| /api/community-activities | 2026-05-09 | ✅ 200 |
| /api/renovation-requests | 2026-05-09 | ✅ 200 |
| /api/express-records | 2026-05-09 | ✅ 200 |
| /api/roles | 2026-05-09 | ✅ 200 |
| /api/users | 2026-05-09 | ✅ 200 |

---

_创建时间：2026-05-09_
_最后更新：2026-05-09_

---

## 九、MySQL 连接字符串强制规范（防止中文乱码）

### 硬性要求
所有服务的 MySQL 连接字符串**必须**包含以下参数：

```
CharSet=utf8mb4;Pooling=false
```

### 正确示例
```csharp
// ✅ 正确
new MySqlConnection("Server=localhost;Port=3306;Database=wo_property;User=woproperty;Password=WOProperty2026!;CharSet=utf8mb4;Pooling=false;");

// ✅ 正确（EF Core）
options.UseMySQL("Server=localhost;Port=3306;Database=wo_property;User=woproperty;Password=WOProperty2026!;CharSet=utf8mb4;Pooling=false");
```

### 错误示例
```csharp
// ❌ 错误 - 缺少 CharSet
new MySqlConnection("Server=localhost;Port=3306;Database=wo_property;User=woproperty;Password=WOProperty2026!;");

// ❌ 错误 - 缺少 Pooling=false（连接池会导致 session charset 变成 latin1）
new MySqlConnection("Server=localhost;Port=3306;Database=wo_property;User=woproperty;Password=WOProperty2026!;CharSet=utf8mb4;");
```

### 根因说明
- `CharSet=utf8mb4`：确保字符编码为 UTF-8
- `Pooling=false`：禁用连接池，防止 MySQL server 默认 latin1 session charset 污染新连接

### 影响范围
- `MasterDataService` (5019) — Program.cs
- `TicketService` (5002) — Program.cs
- `PersonService` (5018) — 检查是否也需要

---

## 十、中文数据验证脚本

创建 `/scripts/validate_chinese_data.py` 用于验证模块数据是否正常：

```python
import mysql.connector
import sys

def check_table_charset(table, columns):
    conn = mysql.connector.connect(
        host="localhost", user="woproperty", password="WOProperty2026!",
        database="wo_property", charset="utf8mb4",
        init_command="SET NAMES utf8mb4"
    )
    cursor = conn.cursor()
    errors = []
    for col in columns:
        try:
            cursor.execute(f"SELECT COUNT(*) FROM `{table}` WHERE `{col}` IS NOT NULL AND (`{col}` LIKE '%Ã%' OR `{col}` LIKE '%â%' OR `{col}` LIKE '%Â%')")
            count = cursor.fetchone()[0]
            if count > 0:
                errors.append(f"  {table}.{col}: {count} garbled rows")
        except: pass
    cursor.close()
    conn.close()
    return errors

# 使用示例
errors = check_table_charset('Tickets', ['Title', 'Description'])
if errors:
    print("❌ 乱码数据发现：")
    for e in errors: print(e)
    sys.exit(1)
else:
    print("✅ 数据正常")
```

---

_最后更新：2026-05-09_
