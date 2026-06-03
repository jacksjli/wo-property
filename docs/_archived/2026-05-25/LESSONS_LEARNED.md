# WO-Property 项目经验总结

> 基于 2026-05-10 这天的问题诊断和修复，总结经验教训

---

## 一、这天主要干了什么

### 背景
上线后发现多个服务失联、前端字段硬编码、数据库列名和代码不匹配、跨模块无法关联查询。

### 做的事
1. **逐个修复失联服务** — 找到连接字符串错误（MySQL charset）、EF Core版本冲突
2. **统一字段管理** — 221个 FieldDefinitions + 192条 ModuleFields 配置 + 28条等价映射
3. **字段标准化** — API字段名（ticketCode）和数据库列名（TicketNumber）分开处理
4. **跨模块关联 API** — 按电话聚合 Personnel/Tickets/Visitors/Delivery
5. **前端动态化** — TicketListView 硬编码label改为从 FieldConfig API 读取
6. **准备测试数据** — 25人按部门插入 Personnel + 关联测试数据

---

## 二、核心经验规则（以后要遵守）

### 🔴 规则1：API字段名 ≠ 数据库列名
**问题**：代码里写 `TicketCode`，数据库列名是 `TicketNumber`，运行时 500 错误。

**规则**：
```
API返回字段名  ←→  FieldDefinitions.FieldKey  ←→  数据库真实列名
```
三层永远分开，不假设它们一样。

**做法**：
- 写SQL之前，先 `DESCRIBE 表名` 确认真实列名
- API DTO 字段名和数据库列名独立定义
- 在 FieldDefinitions 里登记的是 FieldKey（逻辑名），不等同于数据库列名

---

### 🔴 规则2：跨模块关联必须先确定"主关联键"
**问题**：12个模块都有电话字段，但列名五花八门（Phone/ContactPhone/VisitorPhone/ResidentPhone…）。

**规则**：
- 每个等价组只选一个 **MasterFieldKey** 作为标准关联键（如 `phone_number`）
- 新增跨模块功能前，先在 `FieldDefinitionEquivalents` 表查有没有已登记的等价字段
- 禁止在代码里硬编码"phone"字符串而不查表

**做法**：
```sql
-- 查询 phone_number 等价组
SELECT MasterFieldKey, EquivalentFieldKey 
FROM FieldDefinitionEquivalents 
WHERE EquivalenceGroup = 'phone_number';
```

---

### 🔴 规则3：服务间调用必须走 Gateway（或明确不走）
**问题**：前端 `linkedService` 直连 PersonService:5018，不走 Gateway，导致路径混乱。

**规则**：
- 除非明确知道某个服务是"独立数据源"，否则所有跨服务调用走 Gateway
- 或者在 `SERVICES_STATUS.md` 和 `WO-Property/SKILL.md` 里明确标注"直连"

**做法**：
```typescript
// 统一用 SERVICES 配置
export const getServiceUrl = (service: keyof typeof SERVICES): string =>
  `${API_BASE_URL}:${SERVICES[service]}`;
```

---

### 🔴 规则4：新增字段"三步走"
**问题**：FieldDefinitions 有字段，但 ModuleFields 没配，前端读不到label。

**规则**（新增字段的标准流程）：
```
Step 1: FieldDefinitions 登记（FieldKey, DisplayName, DataType）
Step 2: FieldDefinitionEquivalents 建立等价映射（如有）
Step 3: ModuleFields 配模块可见性（module, FieldKey, Alias, Visible）
Step 4: 前端 API 端点返回数据时使用 FieldKey
```
禁止只做 Step 1 就交付。

---

### 🔴 规则5：修复Bug先查真实数据库
**问题**：代码里写 `Persons` 表，数据库里实际是 `Personnel`；代码里写 `CreateTime`，实际是 `CreatedAt`。

**规则**：
- 任何数据库相关修复，**先 DESCRIBE 确认，再动手改代码**
- 用 Python/MySQL Connector 直查数据库，不要用 MySQL CLI（CLI 有 latin1 显示问题）
- 禁止只靠读代码推断数据库结构

**快速验证**：
```python
import mysql.connector
conn = mysql.connector.connect(host='localhost', user='woproperty', 
  password='WOProperty2026!', database='wo_property', charset='utf8mb4')
cursor = conn.cursor()
cursor.execute("DESCRIBE 表名")
print([col[0] for col in cursor])  # 打印所有列名
```

---

### 🔴 规则6：MySQL 连接字符串必须指定 charset
**问题**：中文字符存入数据库变乱码（mojibake）。

**规则**：
- 所有 MySQL 连接字符串必须包含 `CharSet=utf8mb4;Pooling=false`
- EF Core 的 `UseMySql()` 必须指定 `serverVersion`

**标准连接字符串**：
```
Server=localhost;Port=3306;Database=wo_property;User=woproperty;Password=WOProperty2026!;CharSet=utf8mb4;Pooling=false;
```

---

### 🔴 规则7：PersonService 作为"人员中心"直连 wo_property 读各模块表
**问题**：一开始想把 Personnel 数据放进 PersonService自己的表，但数据库初始化冲突。

**规则**：
- PersonService 使用 `MySqlConnector` 直连 `wo_property` 数据库
- 不复用其他服务的 DbContext，避免版本冲突
- LinkedRecordsController 的跨模块查询不走 Gateway，直接查各模块表

---

### 🟡 规则8：前端端口以实际运行准
**问题**：文档写5174，实际 npm run dev 是5173。

**规则**：
- 端口信息以 `lsof -i :端口号` 的输出为准
- 文档/注释里标注"当前实际端口"，不写死默认值

---

## 三、禁止事项（红线）
1. ❌ 不查数据库结构就改 SQL
2. ❌ 硬编码字段label（应该从 FieldConfig API 读）
3. ❌ 新增字段只写 FieldDefinitions 不配 ModuleFields
4. ❌ 假设 API 返回的字段名 = 数据库列名
5. ❌ 不用 charset=utf8mb4 连接 MySQL

---

## 四、已登记的等价组（FieldDefinitionEquivalents）
| 等价组 | MasterFieldKey | 说明 |
|--------|---------------|------|
| phone_number | phone_number | 12个等价字段 |
| person_name | person_name | 11个等价字段 |
| contact_name | contact_name | 2个等价字段 |
| building_id | building_id | 2个等价字段 |
| status | status | 1个等价字段 |

新增模块接入前，先查此表。

---

## 五、文档索引
- 字段规范：`docs/FIELD_MODULE_CHECKLIST.md`
- 服务状态：`docs/SERVICES_STATUS.md`
- MySQL乱码修复：`scripts/fix_garbled_data.py`
- SKILL主文件：`~/.openclaw/skills/wo-property/SKILL.md`

---

_2026-05-10 经验总结_
