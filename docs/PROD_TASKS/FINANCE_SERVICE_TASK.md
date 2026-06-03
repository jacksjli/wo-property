# 任务：填充 FinanceService 业务功能

## 服务信息
- 路径：`/Users/mac/Projects/WO-Property-Management/src/WO.Property.FinanceService/`
- 端口：5009
- 数据库：wo_property

## 当前状态
- 有 TenantFinanceController.cs（骨架）
- 有 FinanceModels.cs（实体定义）
- 需要完成全部业务逻辑

## 业务需求
财务管理完整流程：
1. **收入记录** — 物业费/停车费/其他收入
2. **支出记录** — 日常支出/工资/采购
3. **转账记录** — 账户间转账
4. **财务报表** — 按月/季度/年度统计
5. **余额查询** — 各账户余额

## API 端点设计
```
GET    /api/tenant/finance/transactions     — 收支记录列表
POST   /api/tenant/finance/transactions       — 记账（收入/支出）
GET    /api/tenant/finance/transactions/{id}  — 记录详情
PUT    /api/tenant/finance/transactions/{id}   — 更新记录
DELETE /api/tenant/finance/transactions/{id}   — 删除记录

GET    /api/tenant/finance/accounts           — 账户列表
POST   /api/tenant/finance/accounts             — 创建账户
GET    /api/tenant/finance/accounts/{id}        — 账户详情
GET    /api/tenant/finance/balance              — 账户余额

GET    /api/tenant/finance/reports/monthly     — 月度报表
GET    /api/tenant/finance/reports/quarterly   — 季度报表
GET    /api/tenant/finance/reports/yearly       — 年度报表
```

## 设计原则
1. 使用 TenantDbContextFactory 动态切换租户数据库
2. 所有 API 需要 X-Project header 过滤
3. 完成后 rebuild 并验证服务启动正常

## 输出
完成后汇报：实现了哪些 API、数据库是否需修改、服务是否正常启动