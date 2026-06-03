# 任务：填充 ComplaintService 业务功能

## 服务信息
- 路径：`/Users/mac/Projects/WO-Property-Management/src/WO.Property.ComplaintService/`
- 端口：5201
- 数据库：wo_property（已有表）

## 当前状态
- 已有 TenantComplaintController.cs（骨架）
- 已有 ComplaintModels.cs（实体定义）
- 需要完成全部业务逻辑

## 业务需求
投诉管理完整流程：
1. **投诉提交** — 业主提交投诉（类型、内容、图片、联系方式）
2. **投诉受理** — 物业受理投诉，分配处理人
3. **处理中** — 处理人处理，填写处理结果
4. **完成/关闭** — 业主确认或物业关闭
5. **投诉查询** — 按状态/类型/日期/处理人查询

## API 端点设计
```
GET    /api/tenant/complaints              — 投诉列表（分页+筛选）
POST   /api/tenant/complaints              — 提交投诉
GET    /api/tenant/complaints/{id}          — 投诉详情
PUT    /api/tenant/complaints/{id}          — 更新投诉（受理/处理）
DELETE /api/tenant/complaints/{id}          — 删除投诉

POST   /api/tenant/complaints/{id}/accept   — 受理投诉
POST   /api/tenant/complaints/{id}/resolve   — 解决投诉
POST   /api/tenant/complaints/{id}/close     — 关闭投诉
POST   /api/tenant/complaints/{id}/rate      — 业主评价
```

## 数据模型
```csharp
Complaint {
    Id, ProjectCode, Title, Content,
    Type: Service | Environment | Safety | Other,
    Status: Submitted | Accepted | Processing | Resolved | Closed,
    complainantName, complainantPhone,
    HandlerPersonId, HandlerName,
    Result, Remark,
    Rating, RatedAt,
    CreatedAt, UpdatedAt
}
```

## 设计原则
1. 使用 TenantDbContextFactory 动态切换租户数据库
2. 所有 API 需要 X-Project header 过滤
3. 遵循现有代码风格（和 TicketService 一致）
4. 完成后 rebuild 并验证服务启动正常

## 输出
完成后汇报：
- 实现了哪些 API
- 数据库是否需要新增/修改字段
- 服务是否启动正常