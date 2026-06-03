# TODO.md - 微信小程序员工身份登录改造

> 更新时间: 2026-05-30 03:00
> 状态: ✅ 全部完成

## 任务状态

### 后端 (4个任务)
- [x] Task 1: AuthService `POST /api/auth/login-by-person` ✅ (编译通过，端口5106运行中)
- [x] Task 2: PersonService `GET /api/tenant/persons/by-name-phone` ✅ (编译通过，端口5018运行中)
- [x] Task 3: TicketService `GET /api/tenant/tickets` 增加 areaId/buildingId/ticketTypeId/creatorPersonId 筛选参数 ✅ (编译通过，端口5102运行中)
- [x] Task 4: DispatchService `GET /api/tenant/dispatch/pending` 增加 personId 筛选参数 ✅ (编译通过，端口5241运行中)

### 前端 (5个任务)
- [x] Task 5: 登录页 `pages/login/index.vue` 支持姓名+电话登录 + 微信授权切换 ✅
- [x] Task 6: 新增 `personAuth.js` API 模块 ✅
- [x] Task 7: `dispatch.js` getPending 支持 personId 参数 ✅
- [x] Task 8: 管理页面 `admin/tickets.vue` 增加区域/楼栋/工单类型筛选器（areaId必选，buildingId/ticketTypeId可选） ✅
- [x] Task 9: 工程师页面 `ticket/list.vue` getPending 传入 personId ✅

## API 验证结果
- Task 1: login-by-person (Guest) → employeeType=Guest ✅
- Task 2: by-name-phone → id=26 name=张工程 ✅
- Task 3: areaId=8 filter → 正常返回 ✅
- Task 4: personId=30 filter → 正常返回 ✅

## 后续任务（v1.2 新增）

| 任务 | 状态 | 说明 |
|------|------|------|
| WebSocket 实时刷新 | ✅ 已完成 | 工单创建/状态变更实时推送 |
| 时区修复 | ✅ 已完成 | UTC→北京时间（+8小时） |
| PM2 进程管理 | ✅ 已完成 | 20个服务全部 PM2 管理 |
| 开机自启动 | ⏳ 待配置 | 需执行 `sudo pm2 startup launchd` |

## 创建的文件
- `/src/woa-property-mini/src/api/personAuth.js`
- `/src/woa-property-mini/src/components/custom-tabbar.vue`
- `/src/woa-property-mini/src/pages/main/index.vue`
- `/tmp/mini_person_login_tasks.md` (任务说明)

## 修改的文件
- `/src/woa-property-mini/src/pages/login/index.vue` (重写)
- `/src/woa-property-mini/src/api/dispatch.js` (修复getPending)
- `/src/woa-property-mini/src/api/ticket.js` (新增getTickets)
- `/src/woa-property-mini/src/pages/admin/tickets.vue` (重写，添加筛选器)
- `/src/woa-property-mini/src/pages/ticket/list.vue` (修复personId传入)