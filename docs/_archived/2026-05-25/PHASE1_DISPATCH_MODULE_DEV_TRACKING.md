# 派单模块开发步骤详细清单 v1.0

> 创建时间：2026-05-23  
> 最后更新：2026-05-23  
> 状态：规划中

---

## 📋 开发步骤总览

| Step | 内容 | 状态 | 整合模块 |
|------|------|------|----------|
| 1 | 数据库表创建（6张） | ⏳ 待开始 | 无 |
| 2 | DispatchService 代码审查 | ⏳ 待开始 | 无 |
| 3 | 自动派单 API | ⏳ 待开始 | TicketService, PersonService, MasterDataService |
| 4 | 负载均衡策略 | ⏳ 待开始 | person_workload 表 |
| 5 | 小程序通知集成 | ⏳ 待开始 | NotificationService |
| 6 | 转单审批 API | ⏳ 待开始 | dispatch_records, transfer_requests |
| 7 | 完工确认 API | ⏳ 待开始 | dispatch_records |
| 8 | 满意度评价 API | ⏳ 待开始 | satisfaction_ratings |
| 9 | 超时监控与升级 | ⏳ 待开始 | TicketService, NotificationService |
| 10 | 电脑端页面开发 | ⏳ 待开始 | admin-portal |
| 11 | 小程序页面开发 | ⏳ 待开始 | WO Property Mini |

---

## Step 1：数据库表创建

### 1.1 任务说明
在 `wo_property` 数据库中创建 6 张新表

### 1.2 涉及文件
- 数据库：wo_property
- 建表脚本：待确认

### 1.3 交付物

| # | 表名 | 说明 | SQL 文件 |
|---|------|------|----------|
| 1 | dispatch_records | 派单记录表 | 创建 SQL |
| 2 | transfer_requests | 转单申请记录表 | 创建 SQL |
| 3 | dispatch_rules | 派单规则表 | 创建 SQL |
| 4 | person_workload | 人员负载统计表 | 创建 SQL |
| 5 | satisfaction_ratings | 满意度评价表 | 创建 SQL |
| 6 | timeout_alerts | 超时告警记录表 | 创建 SQL |

### 1.4 详细字段

#### 1.4.1 dispatch_records（派单记录表）
```
字段：
- id: BIGINT, PK, AUTO_INCREMENT
- ticket_id: BIGINT, NOT NULL, 索引
- ticket_code: VARCHAR(50), NOT NULL
- dispatch_time: DATETIME, NOT NULL
- from_person_id: INT, NOT NULL, DEFAULT 0
- from_person_name: VARCHAR(50)
- to_person_id: INT, NOT NULL
- to_person_name: VARCHAR(50)
- status: VARCHAR(20), NOT NULL, DEFAULT 'Pending'
  (值: Pending/WReceived/WProcessing/Completed/Transferred)
- source: VARCHAR(20), NOT NULL, DEFAULT 'Auto'
  (值: Auto/Manual/Transfer)
- workflow_instance_id: VARCHAR(50)
- completed_at: DATETIME
- confirmed_at: DATETIME
- confirmed_by: INT
- confirmed_by_name: VARCHAR(50)
- rating_id: BIGINT
- tenant_code: VARCHAR(50), NOT NULL, 索引
- project_id: INT, NOT NULL, 索引
- created_at: DATETIME, NOT NULL
- updated_at: DATETIME
```

#### 1.4.2 transfer_requests（转单申请记录表）
```
字段：
- id: BIGINT, PK, AUTO_INCREMENT
- dispatch_record_id: BIGINT, NOT NULL
- ticket_id: BIGINT, NOT NULL, 索引
- ticket_code: VARCHAR(50), NOT NULL
- from_person_id: INT, NOT NULL
- from_person_name: VARCHAR(50)
- to_person_id: INT, NOT NULL
- to_person_name: VARCHAR(50)
- reason: VARCHAR(500), NOT NULL
- status: VARCHAR(20), NOT NULL, DEFAULT 'Pending'
  (值: Pending/Approved/Rejected)
- approved_by: INT
- approved_by_name: VARCHAR(50)
- approved_at: DATETIME
- approved_reason: VARCHAR(500)
- transfer_dispatch_id: BIGINT
- tenant_code: VARCHAR(50), NOT NULL, 索引
- project_id: INT, NOT NULL, 索引
- created_at: DATETIME, NOT NULL
- updated_at: DATETIME
```

#### 1.4.3 dispatch_rules（派单规则表）
```
字段：
- id: BIGINT, PK, AUTO_INCREMENT
- rule_name: VARCHAR(100), NOT NULL
- priority: INT, NOT NULL, DEFAULT 0
- ticket_type_id: INT
- ticket_type_name: VARCHAR(50)
- area_id: INT
- area_name: VARCHAR(50)
- person_id: INT
- person_name: VARCHAR(50)
- balance_strategy: VARCHAR(20), NOT NULL, DEFAULT 'LeastWorkload'
  (值: LeastWorkload/RoundRobin/SkillScore)
- is_active: TINYINT, NOT NULL, DEFAULT 1
- tenant_code: VARCHAR(50), NOT NULL, 索引
- project_id: INT, NOT NULL, 索引
- created_at: DATETIME, NOT NULL
- updated_at: DATETIME
```

#### 1.4.4 person_workload（人员负载统计表）
```
字段：
- person_id: INT, PK
- person_name: VARCHAR(50)
- active_ticket_count: INT, NOT NULL, DEFAULT 0
- total_dispatched: INT, NOT NULL, DEFAULT 0
- total_completed: INT, NOT NULL, DEFAULT 0
- last_dispatch_time: DATETIME
- tenant_code: VARCHAR(50), NOT NULL, 索引
- project_id: INT, NOT NULL, 索引
- updated_at: DATETIME
```

#### 1.4.5 satisfaction_ratings（满意度评价表）
```
字段：
- id: BIGINT, PK, AUTO_INCREMENT
- ticket_id: BIGINT, NOT NULL, 索引
- ticket_code: VARCHAR(50), NOT NULL
- dispatch_record_id: BIGINT, NOT NULL
- rater_id: INT, NOT NULL
- rater_name: VARCHAR(50)
- ratee_id: INT, NOT NULL
- ratee_name: VARCHAR(50)
- quality_score: TINYINT, NOT NULL, DEFAULT 5 (1-5)
- attitude_score: TINYINT, NOT NULL, DEFAULT 5 (1-5)
- timeliness_score: TINYINT, NOT NULL, DEFAULT 5 (1-5)
- overall_score: TINYINT, NOT NULL, DEFAULT 5 (1-5)
- comment: VARCHAR(500)
- images: VARCHAR(1000)
- rated_at: DATETIME, NOT NULL
- is_auto_rated: TINYINT, NOT NULL, DEFAULT 0
- tenant_code: VARCHAR(50), NOT NULL, 索引
- project_id: INT, NOT NULL, 索引
- created_at: DATETIME, NOT NULL
- updated_at: DATETIME
```

#### 1.4.6 timeout_alerts（超时告警记录表）
```
字段：
- id: BIGINT, PK, AUTO_INCREMENT
- ticket_id: BIGINT, NOT NULL, 索引
- ticket_code: VARCHAR(50), NOT NULL
- dispatch_record_id: BIGINT
- alert_type: VARCHAR(30), NOT NULL
  (值: PendingTimeout/ProcessingTimeout/CompletedTimeout)
- expected_time: DATETIME, NOT NULL
- actual_time: DATETIME
- timeout_minutes: INT, NOT NULL
- level: INT, NOT NULL, DEFAULT 1 (1-4)
- notify_target_id: INT, NOT NULL
- notify_target_name: VARCHAR(50)
- notification_id: BIGINT
- status: VARCHAR(20), NOT NULL, DEFAULT 'Pending'
  (值: Pending/Sent/Read/Processed)
- sent_at: DATETIME
- processed_at: DATETIME
- tenant_code: VARCHAR(50), NOT NULL
- project_id: INT, NOT NULL
- created_at: DATETIME, NOT NULL
```

### 1.5 验证方法
```sql
-- 检查 6 张表是否创建成功
SHOW TABLES LIKE 'dispatch_records';
SHOW TABLES LIKE 'transfer_requests';
SHOW TABLES LIKE 'dispatch_rules';
SHOW TABLES LIKE 'person_workload';
SHOW TABLES LIKE 'satisfaction_ratings';
SHOW TABLES LIKE 'timeout_alerts';

-- 检查表结构
DESCRIBE dispatch_records;
DESCRIBE transfer_requests;
-- ... 其他表同样检查
```

### 1.6 预计时间
30 分钟

### 1.7 责任人
数据库工程师

### 1.8 状态记录
| 版本 | 日期 | 变更内容 | 确认人 |
|------|------|----------|--------|
| v1.0 | 2026-05-23 | 初始创建 | - |
| | | | |

---

## Step 2：DispatchService 代码审查

### 2.1 任务说明
审查 DispatchService (5241) 现有代码，确认是否满足派单需求

### 2.2 涉及文件
- /src/WO.Property.DispatchService/

### 2.3 交付物
- 代码审查报告（是否有可复用代码、需要改进的点）

### 2.4 详细检查项

#### 2.4.1 项目结构
```
WO.Property.DispatchService/
├── Controllers/
│   └── TenantDispatchController.cs  (检查现有 API)
├── Models/
│   └── DispatchModels.cs            (检查现有模型)
├── Data/
│   └── DispatchDbContext.cs         (检查 DbContext)
├── Middleware/
│   └── TenantRoutingMiddleware.cs   (检查中间件)
├── Program.cs                       (检查服务配置)
└── appsettings.json
```

#### 2.4.2 检查内容
| # | 检查项 | 现状 | 需要改进 |
|---|--------|------|----------|
| 1 | 是否有 TenantDispatchController | ? | 是/否 |
| 2 | 是否有多租户中间件 | ? | 是/否 |
| 3 | 是否有 DbContextFactory | ? | 是/否 |
| 4 | API 路由是否正确 | ? | 是/否 |
| 5 | 模型是否对齐数据库 | ? | 是/否 |

### 2.5 验证方法
```bash
# 检查文件是否存在
ls -la /src/WO.Property.DispatchService/Controllers/

# 编译检查
cd /src/WO.Property.DispatchService
dotnet build
```

### 2.6 预计时间
20 分钟

### 2.7 责任人
后端工程师

### 2.8 状态记录
| 版本 | 日期 | 审查结果 | 确认人 |
|------|------|----------|--------|
| v1.0 | 2026-05-23 | 初始创建 | - |
| | | | |

---

## Step 3：自动派单 API

### 3.1 任务说明
实现自动派单核心逻辑

### 3.2 涉及文件
- DispatchService/Controllers/TenantDispatchController.cs
- DispatchService/Services/DispatchService.cs
- DispatchService/Services/PersonMatchingService.cs

### 3.3 交付物
| # | 文件 | 说明 |
|---|------|------|
| 1 | POST /api/tenant/dispatch/auto | 自动派单 API |
| 2 | DispatchService.cs | 派单服务类 |
| 3 | PersonMatchingService.cs | 人员匹配服务 |

### 3.4 API 规格

#### 3.4.1 POST /api/tenant/dispatch/auto

**请求体：**
```json
{
  "ticketId": 123,
  "ticketCode": "WO-20260523-1040",
  "ticketTypeId": 1,
  "ticketTypeName": "水工维修",
  "areaId": 1,
  "areaName": "东区",
  "buildingId": 2,
  "buildingName": "3号楼",
  "roomId": 3,
  "roomName": "201室",
  "projectId": 1
}
```

**响应：**
```json
{
  "success": true,
  "data": {
    "dispatchRecordId": 456,
    "personId": 10,
    "personName": "张三",
    "status": "Pending",
    "dispatchTime": "2026-05-23T14:00:00"
  },
  "message": "派单成功"
}
```

**错误响应：**
```json
{
  "success": false,
  "message": "没有符合条件的维修人员"
}
```

### 3.5 业务逻辑

```
1. 接收工单创建事件（或前端调用）
       ↓
2. 调用 PersonService 获取候选人员
   - 条件：工种匹配(ticketTypeId in specialtyIds)
   - 条件：负责区域匹配(areaId in areaIds)
       ↓
3. 调用 PersonService 获取每个人的 openid
       ↓
4. 查询 person_workload 表，获取当前工单数
       ↓
5. 负载均衡策略选人（LeastWorkload）
       ↓
6. 写入 dispatch_records 表
       ↓
7. 更新 person_workload 表（active_ticket_count + 1）
       ↓
8. 调用 NotificationService 发送小程序通知
       ↓
9. 返回结果
```

### 3.6 验证方法
```bash
# 测试派单 API
curl -X POST http://localhost:5241/api/tenant/dispatch/auto \
  -H "Content-Type: application/json" \
  -d '{
    "ticketId": 123,
    "ticketCode": "WO-20260523-TEST",
    "ticketTypeId": 1,
    "ticketTypeName": "水工维修",
    "areaId": 1,
    "areaName": "东区",
    "buildingId": 2,
    "buildingName": "3号楼",
    "roomId": 3,
    "roomName": "201室",
    "projectId": 1
  }'

# 验证 dispatch_records 表
SELECT * FROM dispatch_records WHERE ticket_code = 'WO-20260523-TEST';

# 验证 person_workload 表
SELECT * FROM person_workload WHERE active_ticket_count > 0;
```

### 3.7 预计时间
2 小时

### 3.8 责任人
后端工程师

### 3.9 状态记录
| 版本 | 日期 | 变更内容 | 确认人 |
|------|------|----------|--------|
| v1.0 | 2026-05-23 | 初始创建 | - |
| | | | |

---

## Step 4：负载均衡策略

### 4.1 任务说明
实现多种负载均衡策略

### 4.2 涉及文件
- DispatchService/Services/BalanceStrategies/
  - IBalanceStrategy.cs
  - LeastWorkloadStrategy.cs
  - RoundRobinStrategy.cs
  - SkillScoreStrategy.cs

### 4.3 交付物

| # | 文件 | 说明 |
|---|------|------|
| 1 | IBalanceStrategy.cs | 策略接口 |
| 2 | LeastWorkloadStrategy.cs | 最少派单策略 |
| 3 | RoundRobinStrategy.cs | 轮询策略 |
| 4 | SkillScoreStrategy.cs | 技能评分策略 |

### 4.4 策略规格

#### 4.4.1 LeastWorkloadStrategy（最少派单）
```csharp
// 选择当前待处理工单数最少的人
var selected = candidates
    .OrderBy(p => p.ActiveTicketCount)
    .ThenBy(p => p.LastDispatchTime ?? DateTime.MinValue)
    .First();
```

#### 4.4.2 RoundRobinStrategy（轮询）
```csharp
// 轮流分配，每次选下一个
var lastPersonId = await GetLastDispatchedPersonId();
var ordered = candidates.OrderBy(p => p.Id).ToList();
var lastIndex = ordered.FindIndex(p => p.Id == lastPersonId);
var nextIndex = (lastIndex + 1) % ordered.Count;
return ordered[nextIndex];
```

#### 4.4.3 SkillScoreStrategy（技能评分）
```csharp
// 优先派给该工单类型处理最多的人
var selected = candidates
    .OrderByDescending(p => GetTicketTypeExperience(p.Id, ticketTypeId))
    .ThenBy(p => p.ActiveTicketCount)
    .First();
```

### 4.5 验证方法
```bash
# 测试不同策略
# 通过 dispatch_rules 表配置不同策略，验证选人结果
```

### 4.6 预计时间
1 小时

### 4.7 责任人
后端工程师

### 4.8 状态记录
| 版本 | 日期 | 变更内容 | 确认人 |
|------|------|----------|--------|
| v1.0 | 2026-05-23 | 初始创建 | - |
| | | | |

---

## Step 5：小程序通知集成

### 5.1 任务说明
集成微信小程序订阅消息通知

### 5.2 涉及文件
- NotificationService/Services/WechatNotificationService.cs
- 或新建 DispatchService/Services/WechatDispatchNotifyService.cs

### 5.3 交付物
| # | 文件 | 说明 |
|---|------|------|
| 1 | WechatDispatchNotifyService.cs | 派单通知服务 |
| 2 | 通知模板配置 | 模板 ID 配置 |

### 5.4 通知模板

```
模板标题：工单派单通知
模板ID：待申请

字段映射：
- thing1: 【物业维修】您有一个新的工单
- thing2: 地点（area + building + room）
- thing3: 联系人（name + phone）
- thing4: 详情（ticketType + title）
- time5: 时间（dispatchTime）

点击跳转：/pages/dispatch/detail?id={dispatchRecordId}
```

### 5.5 验证方法
```bash
# 检查 notification_records 表
SELECT * FROM notification_records WHERE dispatch_record_id = ?;
```

### 5.6 预计时间
2 小时（含微信平台配置）

### 5.7 责任人
后端工程师 + 微信小程序工程师

### 5.8 状态记录
| 版本 | 日期 | 变更内容 | 确认人 |
|------|------|----------|--------|
| v1.0 | 2026-05-23 | 初始创建 | - |
| | | | |

---

## Step 6：转单审批 API

### 6.1 任务说明
实现转单申请和审批流程

### 6.2 涉及文件
- DispatchService/Controllers/TenantTransferController.cs

### 6.3 交付物

| # | API | 说明 |
|---|-----|------|
| 1 | POST /api/tenant/dispatch/transfer | 申请转单 |
| 2 | GET /api/tenant/dispatch/transfer/pending | 获取待审批列表 |
| 3 | PUT /api/tenant/dispatch/transfer/{id} | 审批转单 |
| 4 | GET /api/tenant/dispatch/transfer/history | 获取转单历史 |

### 6.4 API 规格

#### 6.4.1 POST /api/tenant/dispatch/transfer
**请求体：**
```json
{
  "dispatchRecordId": 456,
  "toPersonId": 11,
  "toPersonName": "李四",
  "reason": "距离太远，无法及时到达"
}
```

**响应：**
```json
{
  "success": true,
  "data": {
    "transferRequestId": 789,
    "status": "Pending"
  },
  "message": "转单申请已提交，等待审批"
}
```

#### 6.4.2 PUT /api/tenant/dispatch/transfer/{id}
**请求体：**
```json
{
  "action": "approve",  // approve / reject
  "reason": "同意转单"
}
```

**响应：**
```json
{
  "success": true,
  "data": {
    "newDispatchRecordId": 457,
    "status": "Approved"
  },
  "message": "转单已审批通过"
}
```

### 6.5 业务逻辑

```
申请转单：
1. 写入 transfer_requests 表（status=Pending）
2. 通知管理员有新转单申请

审批转单：
1. 更新 transfer_requests 表
   - status = Approved/Rejected
   - approved_by, approved_at, approved_reason
2. 如果同意：
   - 新增 dispatch_records（转单后的派单）
   - 通知目标维修人员
   - 更新原维修人员的 active_ticket_count - 1
   - 更新新维修人员的 active_ticket_count + 1
```

### 6.6 验证方法
```bash
# 1. 申请转单
curl -X POST http://localhost:5241/api/tenant/dispatch/transfer \
  -H "Content-Type: application/json" \
  -d '{
    "dispatchRecordId": 456,
    "toPersonId": 11,
    "toPersonName": "李四",
    "reason": "测试转单原因"
  }'

# 2. 查询待审批
curl http://localhost:5241/api/tenant/dispatch/transfer/pending

# 3. 审批
curl -X PUT http://localhost:5241/api/tenant/dispatch/transfer/1 \
  -H "Content-Type: application/json" \
  -d '{"action": "approve", "reason": "同意"}'

# 4. 验证 transfer_requests 表
SELECT * FROM transfer_requests WHERE id = 1;

# 5. 验证 dispatch_records 表
SELECT * FROM dispatch_records WHERE source = 'Transfer';
```

### 6.7 预计时间
2 小时

### 6.8 责任人
后端工程师

### 6.9 状态记录
| 版本 | 日期 | 变更内容 | 确认人 |
|------|------|----------|--------|
| v1.0 | 2026-05-23 | 初始创建 | - |
| | | | |

---

## Step 7：完工确认 API

### 7.1 任务说明
实现维修人员完工提交和创建人确认流程

### 7.2 涉及文件
- DispatchService/Controllers/TenantDispatchController.cs

### 7.3 交付物

| # | API | 说明 |
|---|-----|------|
| 1 | POST /api/tenant/dispatch/{id}/complete | 维修人员提交完工 |
| 2 | POST /api/tenant/dispatch/{id}/confirm | 创建人确认完工 |
| 3 | POST /api/tenant/dispatch/{id}/auto-confirm | 超时自动确认（定时任务） |

### 7.4 API 规格

#### 7.4.1 POST /api/tenant/dispatch/{id}/complete
**请求体：**
```json
{
  "completionRemark": "已更换水龙头，测试正常"
}
```

**响应：**
```json
{
  "success": true,
  "data": {
    "dispatchRecordId": 456,
    "status": "Completed",
    "completedAt": "2026-05-23T15:00:00"
  },
  "message": "完工已提交，等待确认"
}
```

#### 7.4.2 POST /api/tenant/dispatch/{id}/confirm
**请求体：**
```json
{
  "raterId": 1,
  "raterName": "业主",
  "qualityScore": 5,
  "attitudeScore": 5,
  "timelinessScore": 4,
  "overallScore": 5,
  "comment": "维修及时，服务态度好"
}
```

**响应：**
```json
{
  "success": true,
  "data": {
    "dispatchRecordId": 456,
    "status": "Confirmed",
    "ratingId": 101
  },
  "message": "确认完成，感谢评价"
}
```

### 7.5 业务逻辑

```
完工提交：
1. 更新 dispatch_records
   - status = Completed
   - completed_at = now
2. 更新 person_workload
   - active_ticket_count - 1
   - total_completed + 1
3. 通知工单创建人确认

创建人确认：
1. 写入 satisfaction_ratings 表
2. 更新 dispatch_records
   - status = Confirmed
   - confirmed_at, confirmed_by, rating_id
3. 如果 72 小时未确认：
   - 自动确认 + 默认 5 星评价
   - is_auto_rated = true
```

### 7.6 验证方法
```bash
# 1. 完工提交
curl -X POST http://localhost:5241/api/tenant/dispatch/456/complete \
  -H "Content-Type: application/json" \
  -d '{"completionRemark": "测试完工"}'

# 2. 确认完工
curl -X POST http://localhost:5241/api/tenant/dispatch/456/confirm \
  -H "Content-Type: application/json" \
  -d '{
    "raterId": 1,
    "raterName": "业主",
    "qualityScore": 5,
    "attitudeScore": 5,
    "timelinessScore": 4,
    "overallScore": 5,
    "comment": "很好"
  }'

# 3. 验证
SELECT * FROM dispatch_records WHERE id = 456;
SELECT * FROM satisfaction_ratings WHERE dispatch_record_id = 456;
SELECT * FROM person_workload WHERE person_id = 10;
```

### 7.7 预计时间
1.5 小时

### 7.8 责任人
后端工程师

### 7.9 状态记录
| 版本 | 日期 | 变更内容 | 确认人 |
|------|------|----------|--------|
| v1.0 | 2026-05-23 | 初始创建 | - |
| | | | |

---

## Step 8：满意度评价 API

### 8.1 任务说明
完善满意度评价相关功能

### 8.2 涉及文件
- DispatchService/Controllers/TenantRatingController.cs

### 8.3 交付物

| # | API | 说明 |
|---|-----|------|
| 1 | GET /api/tenant/dispatch/rating/{ticketId} | 获取工单评价 |
| 2 | POST /api/tenant/dispatch/{dispatchId}/rate | 补评价 |
| 3 | GET /api/tenant/dispatch/rating/stats | 评价统计 |

### 8.4 验证方法
```bash
# 获取评价
curl http://localhost:5241/api/tenant/dispatch/rating/123

# 评价统计
curl http://localhost:5241/api/tenant/dispatch/rating/stats?personId=10
```

### 8.5 预计时间
1 小时

### 8.6 责任人
后端工程师

### 8.7 状态记录
| 版本 | 日期 | 变更内容 | 确认人 |
|------|------|----------|--------|
| v1.0 | 2026-05-23 | 初始创建 | - |
| | | | |

---

## Step 9：超时监控与升级

### 9.1 任务说明
实现工单超时监控和升级通知

### 9.2 涉及文件
- TicketService/TicketTimeoutMonitor.cs（后台定时任务）
- NotificationService/Services/TimeoutAlertService.cs

### 9.3 交付物

| # | 功能 | 说明 |
|---|------|------|
| 1 | TicketTimeoutMonitor | 超时扫描后台服务 |
| 2 | 超时规则配置 | Pending:30min, Processing:72h, Completed:72h |
| 3 | 升级通知逻辑 | L1→L2→L3→L4 逐级通知 |

### 9.4 超时规则

| 状态 | 超时时间 | 通知对象 | 升级级别 |
|------|----------|----------|----------|
| Pending（待接收） | 30 分钟 | 维修人员本人 | L1 |
| Received（已接收） | 24 小时 | 管理员 | L2 |
| Processing（处理中） | 72 小时 | 区域主管 | L3 |
| Completed（已完工） | 72 小时（未确认） | 工单创建人 | L1→L4 |

### 9.5 验证方法
```bash
# 检查 timeout_alerts 表
SELECT * FROM timeout_alerts WHERE status = 'Pending';

# 检查升级通知是否发送
SELECT * FROM notification_records WHERE template_id LIKE '%timeout%';
```

### 9.6 预计时间
2 小时

### 9.7 责任人
后端工程师

### 9.8 状态记录
| 版本 | 日期 | 变更内容 | 确认人 |
|------|------|----------|--------|
| v1.0 | 2026-05-23 | 初始创建 | - |
| | | | |

---

## Step 10：电脑端页面开发

### 10.1 任务说明
开发 admin-portal 派单管理页面

### 10.2 涉及文件
- admin-portal/src/views/dispatch/
  - DispatchList.vue（派单列表）
  - TransferApproval.vue（转单审批）
  - RatingManagement.vue（评价管理）
  - TimeoutAlerts.vue（超时告警）
- admin-portal/src/api/dispatch.ts

### 10.3 交付物

| # | 页面 | 说明 |
|---|------|------|
| 1 | DispatchList.vue | 派单列表（待处理/处理中/已完成） |
| 2 | TransferApproval.vue | 转单审批列表和审批功能 |
| 3 | RatingManagement.vue | 评价查看和统计 |
| 4 | TimeoutAlerts.vue | 超时告警列表 |

### 10.4 预计时间
4 小时

### 10.5 责任人
前端工程师

### 10.6 状态记录
| 版本 | 日期 | 变更内容 | 确认人 |
|------|------|----------|--------|
| v1.0 | 2026-05-23 | 初始创建 | - |
| | | | |

---

## Step 11：小程序页面开发

### 11.1 任务说明
开发维修人员小程序工单页面

### 11.2 涉及文件
- WO Property Mini/pages/dispatch/
  - dispatch-list（工单列表）
  - dispatch-detail（工单详情）
  - dispatch-transfer（转单申请）

### 11.3 交付物

| # | 页面 | 说明 |
|---|------|------|
| 1 | dispatch-list | 待处理/处理中/已完成工单列表 |
| 2 | dispatch-detail | 工单详情，含接收/完工/转单按钮 |
| 3 | dispatch-transfer | 转单申请表单 |

### 11.4 预计时间
4 小时

### 11.5 责任人
小程序工程师

### 11.6 状态记录
| 版本 | 日期 | 变更内容 | 确认人 |
|------|------|----------|--------|
| v1.0 | 2026-05-23 | 初始创建 | - |
| | | | |

---

## 📊 开发进度总览

| Step | 内容 | 预计时间 | 状态 | 完成日期 | 确认人 |
|------|------|----------|------|----------|--------|
| 1 | 数据库表创建 | 30min | ⏳ | - | - |
| 2 | DispatchService 代码审查 | 20min | ⏳ | - | - |
| 3 | 自动派单 API | 2h | ⏳ | - | - |
| 4 | 负载均衡策略 | 1h | ⏳ | - | - |
| 5 | 小程序通知集成 | 2h | ⏳ | - | - |
| 6 | 转单审批 API | 2h | ⏳ | - | - |
| 7 | 完工确认 API | 1.5h | ⏳ | - | - |
| 8 | 满意度评价 API | 1h | ⏳ | - | - |
| 9 | 超时监控与升级 | 2h | ⏳ | - | - |
| 10 | 电脑端页面开发 | 4h | ⏳ | - | - |
| 11 | 小程序页面开发 | 4h | ⏳ | - | - |
| | **总计** | **19小时** | | | | |

---

## 📝 核对清单

每完成一个 Step，请核对以下内容：

- [ ] 代码已编写并通过编译
- [ ] API 已测试通过
- [ ] 数据库表/字段已验证
- [ ] 文档已更新
- [ ] 甲方已确认

---

**最后更新：2026-05-23**