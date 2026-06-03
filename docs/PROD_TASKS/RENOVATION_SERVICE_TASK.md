# 任务：填充 RenovationService 业务功能

## 服务信息
- 路径：`/Users/mac/Projects/WO-Property-Management/src/WO.Property.RenovationService/`
- 端口：5521
- 数据库：wo_property（已有表）

## 当前状态
- Program.cs 内嵌逻辑，无独立 Controller
- 需要完整重构为 Controller 模式

## 业务需求
装修管理完整流程：
1. **装修申请** — 业主提交装修申请（房屋、装修内容、时间）
2. **物业审核** — 审核装修方案、资质
3. **装修许可证** — 发放装修许可证
4. **装修巡查** — 定期巡查记录
5. **装修完成** — 验收确认，退还押金

## API 端点设计
```
GET    /api/tenant/renovations              — 装修申请列表
POST   /api/tenant/renovations               — 提交装修申请
GET    /api/tenant/renovations/{id}           — 装修详情
PUT    /api/tenant/renovations/{id}           — 更新装修申请
DELETE /api/tenant/renovations/{id}           — 删除申请

POST   /api/tenant/renovations/{id}/approve  — 审核通过
POST   /api/tenant/renovations/{id}/reject   — 审核拒绝
POST   /api/tenant/renovations/{id}/inspect  — 装修巡查
POST   /api/tenant/renovations/{id}/complete — 装修完成验收
```

## 设计原则
1. 使用 TenantDbContextFactory 动态切换租户数据库
2. 所有 API 需要 X-Project header 过滤
3. 完成后 rebuild 并验证服务启动正常

## 输出
完成后汇报：实现了哪些 API、数据库是否需修改、服务是否正常启动