# 服务状态报告

> 更新时间: 2026-06-02 15:52
> 版本: v1.3（2026-06-02 更新）
> 更新内容: 13个服务业务逻辑填充完成，全部22个服务在线

---

## 📊 服务状态总览

| 状态 | 数量 | 说明 |
|------|------|------|
| ✅ 运行中 | 22 | start-all.sh 管理，开机一键启动 |

---

## ✅ 运行中的服务

| 服务 | 端口 | 状态 | 健康检查 |
|------|------|------|----------|
| GatewayService | 5000 | ✅ | healthy |
| AuthService | 5106 | ✅ | healthy |
| TicketService | 5102 | ✅ | healthy |
| DispatchService | 5241 | ✅ | healthy |
| PersonService | 5018 | ✅ | healthy |
| MasterDataService | 5019 | ✅ | healthy |
| MaterialService | 5504 | ✅ | healthy |
| NotificationService | 5105 | ✅ | healthy |
| PaymentService | 5109 | ✅ | healthy |
| DeviceService | 5530 | ✅ | healthy |
| ContractService | 5501 | ✅ | healthy |
| FinanceService | 5509 | ✅ | healthy |
| InspectionService | 5510 | ✅ | healthy |
| ComplaintService | 5201 | ✅ | healthy |
| KeyService | 5512 | ✅ | healthy |
| VisitorService | 5513 | ✅ | healthy |
| StatisticsService | 5250 | ✅ | healthy |
| MobileService | 5526 | ✅ | healthy |
| CommunityService | 5522 | ✅ | healthy |
| ParkingService | 5525 | ✅ | healthy |
| RenovationService | 5521 | ✅ | healthy |
| admin-portal | 5173 | ✅ | 200 OK |

---

## 🔐 认证授权状态（2026-06-02 更新）

### 服务认证

所有后端服务使用统一鉴权：
- Header: `X-Project` — 项目代码过滤
- Header: `X-Tenant: wo_property` — 租户固定
- JWT: `Authorization: Bearer <token>` — 登录获取

### 测试账号

| 账号 | 密码 | 角色 | 说明 |
|------|------|------|------|
| admin | Admin@123 | 系统管理员 | 全权限 |
| tech | Tech@123 | 技术人员 | 技术支持 |
| user | User@123 | 普通用户 | 业主/租户 |

---

## 📋 业务服务 API 清单（2026-06-02 填充完成）

| 服务 | 端口 | API数 | 主要功能 |
|------|------|-------|----------|
| TicketService | 5102 | 10+ | 工单全流程（创建/派单/接单/处理/完成） |
| DispatchService | 5241 | 6+ | 智能派单规则引擎 |
| VisitorService | 5513 | 8 | 访客登记/入场/出场 |
| NotificationService | 5105 | 10 | 通知发送/已读/模板管理 |
| InspectionService | 5510 | 6 | 巡检计划/任务/记录 |
| KeyService | 5512 | 5 | 钥匙借出/归还 |
| ComplaintService | 5201 | 10 | 投诉提交/受理/处理/评价 |
| MaterialService | 5504 | 9 | 物资入库/出库/库存 |
| DeviceService | 5530 | 9 | 设备台账/报修/维护 |
| StatisticsService | 5250 | 5 | 运营报表/工单统计/趋势 |
| ParkingService | 5525 | 14 | 车位管理/停车入场出场/费用 |
| PaymentService | 5109 | 9 | 账单管理/支付/退款 |
| ContractService | 5501 | 12 | 合同全流程/激活/终止/续约 |
| RenovationService | 5521 | 9 | 装修申请/审核/巡查/验收 |
| MobileService | 5526 | 4 | 移动端聚合数据 |
| CommunityService | 5522 | 10 | 活动/公告/建议 |
| FinanceService | 5509 | 14 | 收支记账/账户/报表/转账 |

---

## 🚀 启动方式

```bash
# 一键启动所有服务
bash /Users/mac/Projects/WO-Property-Management/start-all.sh

# 一键停止所有服务
bash /Users/mac/Projects/WO-Property-Management/stop-all.sh
```

### 服务地址

| 服务 | 地址 |
|------|------|
| 后端 Gateway | http://localhost:5000 |
| 管理后台 | http://localhost:5173 |
| 手机调试地址 | http://192.168.1.3:5173 |

---

## 📝 备注

- IP 动态检测：start-all.sh 自动检测本机 IP并更新所有配置
- 健康检查：所有服务 /health 端点
- 日志位置：/Users/mac/Projects/WO-Property-Management/logs/
- 数据库：MySQL wo_property（统一数据库）
- 当前本机 IP：192.168.1.3（记录于 .server-ip）