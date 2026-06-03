# 任务：填充 MaterialService 业务功能

## 服务信息
- 路径：`/Users/mac/Projects/WO-Property-Management/src/WO.Property.MaterialService/`
- 端口：5504
- 数据库：wo_property（已有表）

## 当前状态
- 已有 MaterialModels.cs（实体定义）
- Program.cs 内嵌逻辑，无独立 Controller
- 需要完成全部业务逻辑

## 业务需求
物资管理完整流程：
1. **物资入库** — 采购入库（物资名称、数量、单位、经手人）
2. **物资出库** — 领用出库（关联工单、领用人）
3. **库存查询** — 实时库存查询（按分类/名称/仓库）
4. **库存预警** — 低于最低库存自动提醒
5. **物资统计** — 进出库统计

## API 端点设计
```
GET    /api/tenant/materials              — 物资列表（分页+筛选）
POST   /api/tenant/materials               — 添加物资
GET    /api/tenant/materials/{id}         — 物资详情
PUT    /api/tenant/materials/{id}         — 更新物资
DELETE /api/tenant/materials/{id}         — 删除物资

POST   /api/tenant/materials/{id}/instock   — 入库
POST   /api/tenant/materials/{id}/outstock   — 出库
GET    /api/tenant/materials/stock-record   — 进出库记录
GET    /api/tenant/materials/low-stock       — 库存预警
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