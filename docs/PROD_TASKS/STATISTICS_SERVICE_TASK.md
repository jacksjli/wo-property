# 任务：填充 StatisticsService 业务功能

## 服务信息
- 路径：`/Users/mac/Projects/WO-Property-Management/src/WO.Property.StatisticsService/`
- 端口：5250
- 数据库：wo_property（已有表）

## 当前状态
- 无 Controller，Program.cs 内嵌逻辑
- 有 StatisticsModels.cs（部分实体）
- 需要完整重构为 Controller 模式

## 业务需求
统计报表服务（聚合数据，不写核心业务）：
1. **工单统计** — 按时段/类型/状态/人员统计工单量
2. **运营报表** — 今日新增/待处理/处理中/已完成
3. **人员工作量** — 维修工接单数量/完成率
4. **项目对比** — 多项目数据对比（YGHY001 vs YGXY001）

## API 端点设计
```
GET /api/tenant/statistics/overview       — 运营概览
GET /api/tenant/statistics/tickets         — 工单统计
GET /api/tenant/statistics/engineers       — 工程师工作量
GET /api/tenant/statistics/projects         — 项目对比
GET /api/tenant/statistics/trends?days=30  — 趋势数据
```

## 设计原则
1. 使用 TenantDbContextFactory 动态切换租户数据库
2. 所有 API 需要 X-Project header 过滤
3. 完成后 rebuild 并验证服务启动正常

## 输出
完成后汇报：实现了哪些 API、数据库是否需修改、服务是否正常启动