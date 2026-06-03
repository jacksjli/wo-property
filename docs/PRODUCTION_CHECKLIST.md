# WO 物业管理软件 — 上线前检查清单

**制定日期：** 2026-06-02
**最后更新：** 2026-06-02 16:36
**负责人：** 🪽的芦苇
**当前版本：** v1.3

---

## 一、核心业务流程验证（🔴 P0 — 必须通过）

| # | 检查项 | 状态 | 说明 |
|---|--------|------|------|
| 1 | 员工登录 → 项目分配 → 切换项目 → 看到正确数据 | ⬜ | 需完整走一遍 |
| 2 | 业主报修 → 派单 → 接单 → 处理 → 完成 → 评价 | ⬜ | 端到端完整流程 |
| 3 | 管理员派单 → 维修工接单 → 状态同步 → admin 自动刷新 | ✅ | WebSocket 已实现 |
| 4 | 工单类型/区域/楼栋/工种等基础数据各端一致 | ✅ | MasterDataService 统一管理 |
| 5 | 多项目数据完全隔离（YGHY001 vs YGXY001） | ⬜ | 确认 X-Project header 全链路 |

---

## 二、服务完整性（🔴 P0 — 已完成 ✅）

| # | 服务 | 端口 | 当前状态 | 完成日期 |
|---|------|------|----------|----------|
| 1 | VisitorService | 5513 | ✅ 完整（8 API） | 2026-06-02 |
| 2 | NotificationService | 5105 | ✅ 完整（10 API） | 2026-06-02 |
| 3 | InspectionService | 5510 | ✅ 完整（6 API） | 2026-06-02 |
| 4 | KeyService | 5512 | ✅ 完整（5 API） | 2026-06-02 |
| 5 | ComplaintService | 5201 | ✅ 完整（10 API） | 2026-06-02 |
| 6 | MaterialService | 5504 | ✅ 完整（9 API） | 2026-06-02 |
| 7 | DeviceService | 5530 | ✅ 完整（9 API） | 2026-06-02 |
| 8 | StatisticsService | 5250 | ✅ 完整（5 API） | 2026-06-02 |
| 9 | ParkingService | 5525 | ✅ 完整（14 API） | 2026-06-02 |
| 10 | PaymentService | 5109 | ✅ 完整（9 API） | 2026-06-02 |
| 11 | ContractService | 5501 | ✅ 完整（12 API） | 2026-06-02 |
| 12 | RenovationService | 5521 | ✅ 完整（9 API） | 2026-06-02 |
| 13 | FinanceService | 5509 | ✅ 完整（14 API） | 2026-06-02 |
| 14 | MobileService | 5526 | ✅ 完整（4 API） | 2026-06-02 |
| 15 | CommunityService | 5522 | ✅ 完整（10 API） | 2026-06-02 |
| 16 | CleaningService | 5516 | ✅ 完整（7 API） | 2026-06-02 |
| 17 | ExpressService | 5517 | ✅ 完整 | 2026-06-02 |
| 18 | DeliveryService | 5017 | ✅ 完整（5 API） | 2026-06-02 |
| 19 | AnnouncementService | 5511 | ✅ 完整 | 2026-06-02 |

---

## 三、数据库完整性（P1）

| # | 检查项 | 状态 | 说明 |
|---|--------|------|------|
| 1 | 每个服务对应的数据库表是否存在 | ⬜ | ParkingService/RenovationService 表不存在 |
| 2 | 冗余表已清理（PascalCase vs snake_case） | ⬜ | 需再次确认 |
| 3 | 所有表主键/外键/索引完整 | ⬜ | - |

---

## 四、前端 admin-portal（✅ 已完成 2026-06-02）

| # | 模块 | API数 | 状态 |
|---|------|-------|------|
| 1 | complaint | 10 | ✅ |
| 2 | payment | 8 | ✅ |
| 3 | material | 11 | ✅ |
| 4 | notification | 7 | ✅ |
| 5 | parking | 6 | ✅ |
| 6 | device | 完整 | ✅ |
| 7 | contract | 9 | ✅ |
| 8 | renovation | 9 | ✅ |
| 9 | community | 完整 | ✅ |
| 10 | inspection | 7 | ✅ |
| 11 | finance | 完整 | ✅ |
| 12 | statistics | 5 | ✅ |
| 13 | cleaning | 7 | ✅ |
| 14 | express | 完整 | ✅ |
| 15 | delivery | 5 | ✅ |
| 16 | announcement | 完整 | ✅ |

---

## 五、微信小程序（P1）

| # | 检查项 | 状态 | 说明 |
|---|--------|------|------|
| 1 | 登录跳转 operator→mine/index | ✅ | 已修复 |
| 2 | admin 页面 ProjectTabBar | ✅ | dashboard/tickets/dispatch/ticket-detail/alerts |
| 3 | detail.vue 接单/处理/完成按钮 | ✅ | dispatchStatus 判断已修复 |
| 4 | 小程序重新编译上传 | ⬜ | 待执行 |
| 5 | 张维修完整流程验证 | ⬜ | 待手机测试 |

---

## 六、部署配置（P1）

| # | 检查项 | 状态 | 说明 |
|---|--------|------|------|
| 1 | start-all.sh 包含全部 22 个服务 | ✅ | 2026-06-02 已更新 |
| 2 | IP 自动检测机制正常 | ✅ | .server-ip |
| 3 | 看门狗脚本正常 | ✅ | service-watchdog.sh |
| 4 | SERVICES_STATUS.md 最新 | ✅ | 2026-06-02 |
| 5 | DEPLOYMENT.md 最新 | ✅ | 2026-06-02 |

---

## 七、生产环境待处理（P2）

| # | 检查项 | 状态 | 说明 |
|---|--------|------|------|
| 1 | HTTPS/域名（小程序必须） | ⬜ | 未配置 |
| 2 | 多 Mac 部署配置 | ⬜ | 设计已完成，待验证 |
| 3 | 数据库备份策略 | ⬜ | 需配置 |
| 4 | 日志集中收集 | ⬜ | 需配置 |