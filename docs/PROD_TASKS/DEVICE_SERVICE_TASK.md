# 任务：填充 DeviceService 业务功能

## 服务信息
- 路径：`/Users/mac/Projects/WO-Property-Management/src/WO.Property.DeviceService/`
- 端口：5530
- 数据库：wo_property（已有表）

## 当前状态
- 已有 TenantDeviceController.cs（骨架）
- 无独立 Models 文件
- 需要完成全部业务逻辑

## 业务需求
设备管理完整流程：
1. **设备台账** — 设备登记（名称、类型、位置、负责人、状态）
2. **设备巡检** — 定期巡检计划（可关联 InspectionService）
3. **设备维修** — 报修→派单→维修→完成
4. **设备查询** — 按类型/状态/位置查询
5. **设备统计** — 设备运行状态分布

## API 端点设计
```
GET    /api/tenant/devices              — 设备列表（分页+筛选）
POST   /api/tenant/devices              — 添加设备
GET    /api/tenant/devices/{id}         — 设备详情
PUT    /api/tenant/devices/{id}         — 更新设备
DELETE /api/tenant/devices/{id}         — 删除设备

POST   /api/tenant/devices/{id}/repair  — 报修（创建工单）
GET    /api/tenant/devices/stats        — 设备统计
```

## 设计原则
1. 使用 TenantDbContextFactory 动态切换租户数据库
2. 所有 API 需要 X-Project header 过滤
3. 完成后 rebuild 并验证服务启动正常

## 输出
完成后汇报：实现了哪些 API、数据库是否需修改、服务是否正常启动