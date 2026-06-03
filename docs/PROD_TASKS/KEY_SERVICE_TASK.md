# 任务：填充 KeyService 业务功能

## 服务信息
- 路径：`/Users/mac/Projects/WO-Property-Management/src/WO.Property.KeyService/`
- 端口：5012
- 数据库：wo_property（已有表）

## 当前状态
- 已有 TenantKeyController.cs（骨架，只有部分方法）
- 已有 KeyModels.cs（实体定义）
- 需要完成全部业务逻辑

## 业务需求
钥匙管理完整流程：
1. **钥匙入库** — 钥匙登记入库（钥匙名称、类型、存放位置）
2. **钥匙借用** — 业主/员工借钥匙（选择钥匙→填写用途→签借）
3. **钥匙归还** — 归还钥匙（签退）
4. **钥匙查询** — 按状态/类型/位置查询钥匙库存
5. **借用记录** — 所有钥匙的借用历史

## API 端点设计
```
GET    /api/tenant/keys                 — 钥匙列表（支持筛选：状态/类型）
POST   /api/tenant/keys                 — 添加钥匙
GET    /api/tenant/keys/{id}             — 钥匙详情
PUT    /api/tenant/keys/{id}             — 更新钥匙信息
DELETE /api/tenant/keys/{id}             — 删除钥匙

POST   /api/tenant/keys/{id}/borrow      — 借钥匙
POST   /api/tenant/keys/{id}/return      — 归还钥匙
GET    /api/tenant/keys/records          — 借用记录
```

## 数据模型
```csharp
Key {
    Id, ProjectCode, KeyName, KeyType,
    Location, Status: Available | Borrowed | Lost | Retired,
    TotalCount, AvailableCount,
    CreatedAt, UpdatedAt
}

KeyRecord {
    Id, KeyId, ProjectCode,
    BorrowerName, BorrowerPhone,
    BorrowerPersonId,
    BorrowTime, ReturnTime,
    Purpose, Remark,
    Status: Borrowed | Returned
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