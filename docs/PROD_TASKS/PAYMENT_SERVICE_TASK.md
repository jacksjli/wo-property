# 任务：填充 PaymentService 业务功能

## 服务信息
- 路径：`/Users/mac/Projects/WO-Property-Management/src/WO.Property.PaymentService/`
- 端口：5109
- 数据库：wo_property（已有表）

## 当前状态
- 已有 TenantPaymentController.cs（骨架）
- 已有 PaymentModels.cs（实体定义）
- 需要完成全部业务逻辑

## 业务需求
缴费管理完整流程：
1. **缴费项目** — 物业费/停车费/水电费等缴费项
2. **缴费记录** — 业主缴费/退费记录
3. **待缴账单** — 业主查看待缴费用
4. **在线支付** — 预留支付接口（微信/支付宝）
5. **缴费查询** — 按业主/类型/日期/状态查询

## API 端点设计
```
GET    /api/tenant/payments                — 缴费记录列表
POST   /api/tenant/payments                  — 创建缴费记录
GET    /api/tenant/payments/{id}            — 缴费详情
PUT    /api/tenant/payments/{id}            — 更新缴费记录

POST   /api/tenant/payments/{id}/pay        — 确认支付
POST   /api/tenant/payments/{id}/refund     — 退款
GET    /api/tenant/payments/pending         — 待缴费列表
GET    /api/tenant/payments/stats           — 缴费统计
```

## 设计原则
1. 使用 TenantDbContextFactory 动态切换租户数据库
2. 所有 API 需要 X-Project header 过滤
3. 完成后 rebuild 并验证服务启动正常

## 输出
完成后汇报：实现了哪些 API、数据库是否需修改、服务是否正常启动