# 任务：填充 CommunityService 业务功能

## 服务信息
- 路径：`/Users/mac/Projects/WO-Property-Management/src/WO.Property.CommunityService/`
- 端口：5522
- 数据库：wo_property

## 当前状态
- Program.cs 内嵌逻辑，无独立 Controller
- 需要完整重构为 Controller 模式

## 业务需求
社区服务完整流程：
1. **活动管理** — 发布社区活动（标题、内容、时间、地点）
2. **活动报名** — 业主报名参加
3. **公告管理** — 小区公告发布
4. **建议收集** — 业主建议提交
5. **查询统计** — 活动/公告/建议列表

## API 端点设计
```
GET    /api/tenant/community/activities       — 活动列表
POST   /api/tenant/community/activities         — 发布活动
GET    /api/tenant/community/activities/{id}   — 活动详情
PUT    /api/tenant/community/activities/{id}    — 更新活动
DELETE /api/tenant/community/activities/{id}     — 删除活动

POST   /api/tenant/community/activities/{id}/join   — 报名参加
GET    /api/tenant/community/notices            — 公告列表
POST   /api/tenant/community/notices             — 发布公告
GET    /api/tenant/community/suggestions         — 建议列表
POST   /api/tenant/community/suggestions          — 提交建议
```

## 设计原则
1. 使用 TenantDbContextFactory 动态切换租户数据库
2. 所有 API 需要 X-Project header 过滤
3. 完成后 rebuild 并验证服务启动正常

## 输出
完成后汇报：实现了哪些 API、数据库是否需修改、服务是否正常启动