# 任务：填 admin-portal complaint 模块前后端

## 已有后端 API（ComplaintService 端口 5201）
```
GET    /api/tenant/complaints              — 列表（分页+筛选）
POST   /api/tenant/complaints              — 创建
GET    /api/tenant/complaints/{id}          — 详情
PUT    /api/tenant/complaints/{id}           — 更新
DELETE /api/tenant/complaints/{id}           — 删除
POST   /api/tenant/complaints/{id}/accept    — 受理
POST   /api/tenant/complaints/{id}/resolve   — 解决
POST   /api/tenant/complaints/{id}/close     — 关闭
POST   /api/tenant/complaints/{id}/rate      — 评价
GET    /api/tenant/complaints/stats         — 统计
```

## 需要做：
### 1. 创建 API 客户端
文件：`/Users/mac/Projects/WO-Property-Management/src/admin-portal/src/api/complaint.ts`

参考已有文件如 `ticket.ts` 的写法：
- 基础路径：`http://localhost:5000/api/tenant/complaints`
- 使用 stores/project.ts 里的 currentProject.value.code 作为 X-Project header
- 使用 auth store 获取 token
- 实现：getComplaints, getComplaint, createComplaint, updateComplaint, deleteComplaint, acceptComplaint, resolveComplaint, closeComplaint, rateComplaint, getComplaintStats

### 2. 更新页面
文件：`/Users/mac/Projects/WO-Property-Management/src/admin-portal/src/views/complaint/ComplaintList.vue`

在 `<script setup>` 中：
- 导入 api
- 改成用真实 API 获取数据（不要 mock 数据）
- 分页、搜索、详情弹窗、受理/解决/关闭按钮

参考其他已完成的页面（如 ticket/TicketList.vue）

## 输出
完成后汇报：创建了哪些 API 函数、页面是否已对接真实 API