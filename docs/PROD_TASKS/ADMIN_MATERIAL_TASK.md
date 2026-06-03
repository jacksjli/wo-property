# 任务：填 admin-portal material 模块前后端

## 后端 API（MaterialService 端口 5504）
路径前缀：`/api/tenant/materials`
支持：GET列表/POST创建/GET详情/PUT更新/DELETE删除/POST instock/POST outstock/GET stock-record/GET low-stock

## 任务
1. 阅读 `/Users/mac/Projects/WO-Property-Management/src/admin-portal/src/api/material.ts`
2. 参考 `ticket.ts` 补全 `material.ts` 所有函数
3. 阅读 `/Users/mac/Projects/WO-Property-Management/src/admin-portal/src/views/material/MaterialList.vue`
4. 替换 mock 数据为真实 API
5. rebuild：cd /Users/mac/Projects/WO-Property-Management/src/admin-portal && npx vite build 2>&1 | tail -5
6. 汇报结果