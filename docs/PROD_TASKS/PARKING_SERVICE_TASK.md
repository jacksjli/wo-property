# 任务：填充 ParkingService 业务功能

## 服务信息
- 路径：`/Users/mac/Projects/WO-Property-Management/src/WO.Property.ParkingService/`
- 端口：5525
- 数据库：wo_property（已有表）

## 当前状态
- 已有 TenantParkingController.cs（骨架）
- 已有 ParkingModels.cs（实体定义）
- 需要完成全部业务逻辑

## 业务需求
车位管理完整流程：
1. **车位登记** — 车位信息录入（车位号、类型、状态）
2. **车辆登记** — 车辆信息绑定车位
3. **停车记录** — 进出停车场记录
4. **费用计算** — 临停车费/月租车费计算
5. **车位查询** — 按状态/类型/楼栋查询

## API 端点设计
```
GET    /api/tenant/parkings              — 车位列表
POST   /api/tenant/parkings               — 添加车位
GET    /api/tenant/parkings/{id}           — 车位详情
PUT    /api/tenant/parkings/{id}           — 更新车位
DELETE /api/tenant/parkings/{id}           — 删除车位

POST   /api/tenant/parkings/{id}/check-in   — 停车入场
POST   /api/tenant/parkings/{id}/check-out  — 停车出场
GET    /api/tenant/parkings/records         — 停车记录
```

## 设计原则
1. 使用 TenantDbContextFactory 动态切换租户数据库
2. 所有 API 需要 X-Project header 过滤
3. 完成后 rebuild 并验证服务启动正常

## 输出
完成后汇报：实现了哪些 API、数据库是否需修改、服务是否正常启动