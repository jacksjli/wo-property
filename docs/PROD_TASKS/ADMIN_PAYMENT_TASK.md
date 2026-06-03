# 任务：填 admin-portal payment 模块前后端

## 后端 API（PaymentService 端口 5109）
路径前缀：`/api/tenant/payments`
支持：GET列表/POST创建/GET详情/PUT更新/DELETE删除/POST pay/POST refund/GET pending/GET stats

## 任务
1. 阅读 `/Users/mac/Projects/WO-Property-Management/src/admin-portal/src/api/payment.ts` 现有代码
2. 参考 `ticket.ts` 补全 `payment.ts` 所有函数实现
3. 阅读 `/Users/mac/Projects/WO-Property-Management/src/admin-portal/src/views/payment/PaymentList.vue`
4. 替换页面里的 mock 数据为真实 API 调用，保持 UI 不变
5. rebuild：cd /Users/mac/Projects/WO-Property-Management/src/admin-portal && npx vite build 2>&1 | tail -5
6. 汇报结果