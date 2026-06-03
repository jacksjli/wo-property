# 任务：填充 NotificationService 业务功能

## 服务信息
- 路径：`/Users/mac/Projects/WO-Property-Management/src/WO.Property.NotificationService/`
- 端口：5005
- 数据库：wo_property（已有表）

## 当前状态
- 已有 TenantNotificationController.cs（骨架，只有部分方法）
- 已有 NotificationModels.cs（实体定义）
- 需要完成全部业务逻辑

## 业务需求
通知管理完整流程：
1. **通知发送** — 物业向业主/员工发送通知（标题、内容、类型）
2. **通知类型** — 系统通知、缴费提醒、活动通知、故障通知
3. **推送渠道** — 微信服务号/小程序模板消息（预留接口）
4. **已读/未读** — 用户查看通知后标记已读
5. **通知查询** — 按用户/类型/日期/已读状态查询

## API 端点设计
```
GET    /api/tenant/notifications                — 通知列表（支持分页+筛选）
POST   /api/tenant/notifications                — 发送通知
GET    /api/tenant/notifications/{id}            — 通知详情
DELETE /api/tenant/notifications/{id}            — 删除通知

POST   /api/tenant/notifications/{id}/read      — 标记已读
POST   /api/tenant/notifications/read-all        — 全部已读

GET    /api/tenant/notifications/user/{personId} — 获取用户的通知列表
```

## 数据模型
```csharp
Notification {
    Id, ProjectCode,
    Title, Content,
    Type: System | Payment | Activity | Fault,
    TargetType: All | Building | Person,
    TargetId,  // buildingId 或 personId
    IsRead, ReadAt,
    CreatedAt
}
```

## 设计原则
1. 使用 TenantDbContextFactory 动态切换租户数据库
2. 所有 API 需要 X-Project header 过滤
3. 遵循现有代码风格（和 TicketService 一致）
4. 微信推送接口预留（实际推送后续接入微信服务号）
5. 完成后 rebuild 并验证服务启动正常

## 输出
完成后汇报：
- 实现了哪些 API
- 数据库是否需要新增/修改字段
- 服务是否启动正常