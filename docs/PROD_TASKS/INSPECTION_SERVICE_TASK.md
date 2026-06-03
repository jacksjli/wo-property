# 任务：填充 InspectionService 业务功能

## 服务信息
- 路径：`/Users/mac/Projects/WO-Property-Management/src/WO.Property.InspectionService/`
- 端口：5010
- 数据库：wo_property（已有表）

## 当前状态
- 已有 TenantInspectionController.cs（骨架，只有部分方法）
- 已有 InspectionModels.cs（实体定义）
- 需要完成全部业务逻辑

## 业务需求
巡检管理完整流程：
1. **巡检计划** — 制定周期性巡检计划（楼栋/区域/设备类型）
2. **巡检任务派发** — 将计划转为具体的巡检任务指派给巡检员
3. **巡检执行** — 巡检员到现场执行，记录检查项和结果
4. **异常上报** — 发现问题自动创建工单
5. **巡检记录查询** — 按人员/日期/区域/状态查询

## API 端点设计
```
GET    /api/tenant/inspections              — 巡检列表（支持分页+筛选）
POST   /api/tenant/inspections              — 创建巡检计划
GET    /api/tenant/inspections/{id}          — 巡检详情
PUT    /api/tenant/inspections/{id}          — 更新巡检
DELETE /api/tenant/inspections/{id}           — 删除计划

POST   /api/tenant/inspections/{id}/dispatch   — 派发巡检任务
POST   /api/tenant/inspections/{id}/execute    — 执行巡检（记录结果）
POST   /api/tenant/inspections/{id}/complete   — 完成任务
```

## 数据模型
```csharp
Inspection {
    Id, ProjectCode, Title, Description,
    PlanType: Daily | Weekly | Monthly,
    AreaId, BuildingId,
    AssigneePersonId, AssigneeName,
    Status: Planned | Dispatched | InProgress | Completed | Cancelled,
    PlannedDate, CompletedDate,
    CreatedAt, UpdatedAt
}

InspectionRecord {
    Id, InspectionId, ProjectCode,
    CheckItem, CheckResult, Remark, PhotoUrls,
    CreatedAt
}
```

## 设计原则
1. 使用 TenantDbContextFactory 动态切换租户数据库
2. 所有 API 需要 X-Project header 过滤
3. 遵循现有代码风格（和 TicketService 一致）
4. 巡检执行时发现异常可联动创建工单（调用 TicketService API）
5. 完成后 rebuild 并验证服务启动正常

## 输出
完成后汇报：
- 实现了哪些 API
- 数据库是否需要新增/修改字段
- 服务是否启动正常