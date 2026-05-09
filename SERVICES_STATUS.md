# WO物业管理系统 - 服务状态

## 概述
- **总服务数**: 16个微服务
- **状态**: 14个运行中，2个开发中 🚧
- **更新时间**: 2026-05-05

## 服务列表

| 端口 | 服务名称 | 状态 | 功能 |
|------|----------|------|------|
| 5000 | GatewayService | ✅ | API网关，统一入口 |
| 5002 | TicketService | 🚧开发中 | 工单管理（待迁移到PersonService） |
| 5003 | DispatchService | ✅ | 智能派单 |
| 5004 | MaterialService | ✅ | 物料管理 |
| 5005 | NotificationService | ✅ | 通知服务 |
| 5006 | AuthService | ✅ | 用户认证 |
| 5007 | DeviceService | ✅ | 设备管理 |
| 5008 | ContractService | ✅ | 合同管理 |
| 5009 | FinanceService | ✅ | 财务管理 |
| 5010 | InspectionService | ✅ | 巡检管理 |
| 5011 | ComplaintService | ✅ | 投诉管理 |
| 5012 | KeyService | ✅ | 钥匙管理 |
| 5013 | VisitorService | ✅ | 访客管理 |
| 5014 | StatisticsService | ✅ | 统计服务 |
| 5015 | MobileService | ✅ | 移动端服务 |
| 5018 | PersonService | 🚧开发中 | 统一人员中心（新建）|
| 5019 | MasterDataService | 🚧开发中 | 基础数据+字段管理（升级）|

## API基础URL（更新后）
- 本地: `http://localhost:端口/api`
- PersonService: 5018
- MasterDataService: 5019
- 认证: 所有服务使用统一JWT认证

## JWT配置
- Issuer: `wo-property-unified-auth`
- Audience: `wo-property-services`
- SecretKey: `WO-Property-Management-Unified-Secret-Key-2026-For-All-Services`

## 架构决策记录 (2026-05-05)

### 决策 1：PersonService 和 MasterDataService 必须实现
- 之前这两个服务不存在，只有目录
- 现在需要真正实现
- 所有服务共享 PersonService 的人员数据

### 决策 2：MySQL 统一
- 所有服务从 SQLite 迁移到 MySQL
- 数据库名：wo_property
- 迁移时间表：6 周

### 决策 3：字段管理模块放 MasterDataService
- 所有字段定义统一存储
- 模块使用字段必须从下拉选择
- 只有系统管理员可以新增字段

### 决策 4：服务间通信协议
- 业务服务间禁止直接调用
- 只能调用公共服务（PersonService, MasterDataService）
- 使用 HTTP + JWT Bearer Token

## 当前进度

### Week 1 进行中
- [x] 架构文档编写（已完成 10 份文档）
- [ ] PersonService (5018) 开发
- [ ] MasterDataService (5019) 字段管理 API
- [ ] MySQL 数据库初始化
- [ ] 前端 fieldConfig.ts 重构

### 子代理任务状态
- 子代理 1：MasterDataService 字段管理 API（进行中）
- 子代理 2：前端 fieldConfig.ts 重构（进行中）
- 子代理 3：PersonService 开发（进行中）
- 子代理 4：MySQL 数据库初始化（进行中）

## 测试账号
- admin / Admin@123 (管理员)
- tech / Tech@123 (技术人员)
- user / User@123 (普通用户)

## 管理页面
- HTML测试页面: `~/Projects/WO-Property-Management/test-login.html`
