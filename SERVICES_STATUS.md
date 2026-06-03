# WO物业管理系统 - 服务状态

## 概述
- **总服务数**: 16个微服务 + 1个前端
- **状态**: 全部运行中 ✅
- **更新时间**: 2026-05-25

## 服务端口映射（唯一数据源）

> ⚠️ **重要**: 端口以 `config/ports.json` 为准，本文仅供参考
> 所有服务均已实现 `/health` 健康检查端点

| 端口 | 服务名称 | 状态 | 功能 |
|------|----------|------|------|
| 5000 | GatewayService | ✅ | API网关，统一入口 |
| 5106 | AuthService | ✅ | 用户认证服务 |
| 5102 | TicketService | ✅ | 工单管理服务 |
| 5241 | DispatchService | ✅ | 智能派单服务 |
| 5018 | PersonService | ✅ | 统一人员中心 |
| 5019 | MasterDataService | ✅ | 基础数据+字段管理 |
| 5504 | MaterialService | ✅ | 物料管理 |
| 5005 | NotificationService | ✅ | 通知服务 |
| 5530 | DeviceService | ✅ | 设备管理 |
| 5501 | ContractService | ✅ | 合同管理 |
| 5509 | FinanceService | ✅ | 财务管理 |
| 5510 | InspectionService | ✅ | 巡检管理 |
| 5011 | ComplaintService | ✅ | 投诉管理 |
| 5512 | KeyService | ✅ | 钥匙管理 |
| 5513 | VisitorService | ✅ | 访客管理 |
| 5250 | StatisticsService | ✅ | 统计服务 |
| 5526 | MobileService | ✅ | 移动端服务 |
| 5173 | admin-portal | ✅ | 前端管理后台 |

## API基础URL
- 本地: `http://localhost:端口/api`
- 统一入口(Gateway): `http://localhost:5000`

## JWT配置
- Issuer: `wo-property-unified-auth`
- Audience: `wo-property-services`
- SecretKey: `WO-Property-Management-Unified-Secret-Key-2026-For-All-Services`

## 测试账号
- admin / Admin@123 (管理员)
- tech / Tech@123 (技术人员)
- user / User@123 (普通用户)

## 管理页面
- 前端: http://localhost:5173/project
- API文档: http://localhost:5000/swagger

## 启动脚本
```bash
# 方式1: 使用启动脚本（推荐）
bash scripts/start-services.sh

# 方式2: 手动启动（使用 config/ports.json 中的端口）
```

## 端口映射表
详细端口配置见: `config/ports.json`