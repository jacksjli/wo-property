# 任务：填充 MobileService 业务功能

## 服务信息
- 路径：`/Users/mac/Projects/WO-Property-Management/src/WO.Property.MobileService/`
- 端口：5526
- 数据库：wo_property

## 当前状态
- Program.cs 内嵌逻辑，无独立 Controller
- 需要完整重构为 Controller 模式

## 业务需求
移动端聚合服务（只聚合数据，不写核心业务）：
1. **我的工单** — 聚合当前用户相关工单
2. **我的通知** — 聚合当前用户通知
3. **我的待办** — 聚合待处理事项
4. **首页数据** — 聚合运营数据展示

## API 端点设计
```
GET /api/tenant/mobile/dashboard     — 首页数据
GET /api/tenant/mobile/tickets        — 我的工单
GET /api/tenant/mobile/notifications   — 我的通知
GET /api/tenant/mobile/todos           — 我的待办
```

## 设计原则
1. 使用 TenantDbContextFactory 动态切换租户数据库
2. 通过 HttpClient 调用其他服务获取数据
3. 所有 API 需要 X-Project header 过滤
4. 完成后 rebuild 并验证服务启动正常

## 输出
完成后汇报：实现了哪些 API、数据库是否需修改、服务是否正常启动