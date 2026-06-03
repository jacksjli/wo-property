# 任务：填充 VisitorService 业务功能

## 服务信息
- 路径：`/Users/mac/Projects/WO-Property-Management/src/WO.Property.VisitorService/`
- 端口：5013
- 数据库：wo_property（已有表）

## 当前状态
- 已有 TenantVisitorController.cs（骨架，只有1-2个方法）
- 已有 VisitorModels.cs（实体定义）
- 需要完成全部业务逻辑

## 业务需求
访客管理完整流程：
1. **访客预约** — 业主提交访客预约（姓名、电话、来访时间、来访事由）
2. **预约审核** — 物业审核通过/拒绝
3. **访客登记** — 访客到场登记（身份证、头像照片）
4. **签入签出** — 进门签到、出门签退
5. **访客记录查询** — 按日期/楼栋/状态查询

## API 端点设计
```
GET    /api/tenant/visitors           — 访客列表（支持分页+筛选）
POST   /api/tenant/visitors          — 创建访客预约
GET    /api/tenant/visitors/{id}      — 访客详情
PUT    /api/tenant/visitors/{id}      — 更新访客（审核/修改）
DELETE /api/tenant/visitors/{id}      — 删除预约

POST   /api/tenant/visitors/{id}/checkin   — 签入
POST   /api/tenant/visitors/{id}/checkout  — 签出
```

## 数据模型
```csharp
Visitor {
    Id, ProjectCode, BuildingId, RoomNumber,
    VisitorName, VisitorPhone, VisitorIdCard,
    VisitPurpose, ExpectedArrivalTime,
    CheckInTime, CheckOutTime,
    Status: Pending | Approved | Rejected | CheckedIn | CheckedOut | Cancelled,
    CreatedAt, UpdatedAt, CreatorPersonId
}
```

## 设计原则
1. 使用 TenantDbContextFactory 动态切换租户数据库
2. 所有 API 需要 X-Project header 过滤
3. 遵循现有代码风格（和 TicketService 一致）
4. 完成后 rebuild 并验证服务启动正常
5. 记录任何 schema 需要

## 输出
完成后汇报：
- 实现了哪些 API
- 数据库是否需要新增/修改字段
- 服务是否启动正常