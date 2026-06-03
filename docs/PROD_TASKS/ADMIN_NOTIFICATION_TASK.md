# 任务：填 admin-portal notification 模块前后端

## 后端 API（NotificationService 端口 5105）
路径前缀：`/api/tenant/notifications`
支持：GET列表/POST发送/GET详情/PUT更新/DELETE删除/POST mark-read/GET stats

## 任务
1. 阅读 `/Users/mac/Projects/WO-Property-Management/src/admin-portal/src/api/notification.ts`
2. 参考 `ticket.ts` 补全 `notification.ts` 所有函数
3. 阅读 `/Users/mac/Projects/WO-Property-Management/src/admin-portal/src/views/notification/NotificationList.vue`
4. 替换 mock 数据为真实 API
5. rebuild：cd /Users/mac/Projects/WO-Property-Management/src/admin-portal && npx vite build 2>&1 | tail -5
6. 汇报结果