# WO物业管理软件 - 项目结构

## 项目概述

**WO物业管理软件** 是一套基于 .NET 8 微服务架构的物业管理平台，采用前后端分离设计，支持多端访问。

## 技术栈

### 后端
- **.NET 8** - 微服务框架
- **ASP.NET Core Web API** - RESTful API
- **Entity Framework Core** - ORM
- **SQLite** - 开发环境数据库
- **PostgreSQL** - 生产环境数据库
- **JWT** - 统一身份认证

### 前端
- **Vue 3** - 前端框架
- **TypeScript** - 类型系统
- **Element Plus** - UI组件库
- **Vite** - 构建工具
- **Pinia** - 状态管理
- **Vue Router** - 路由管理

### 基础设施
- **Docker** - 容器化
- **Nginx** - 反向代理
- **Redis** - 缓存（可选）

## 项目目录结构

```
WO-Property-Management/
├── docker/                          # Docker配置
│   ├── docker-compose.yml          # 容器编排
│   ├── nginx.conf                  # Nginx配置
│   ├── Dockerfile.dotnet           # .NET服务Dockerfile模板
│   └── README.md                  # 部署说明
│
├── src/                            # 源代码目录
│   │
│   ├── admin-portal/              # Vue管理后台
│   │   ├── src/
│   │   │   ├── api/              # API调用
│   │   │   ├── assets/           # 静态资源
│   │   │   ├── components/       # 公共组件
│   │   │   ├── layouts/          # 布局组件
│   │   │   ├── router/           # 路由配置
│   │   │   ├── stores/           # 状态管理
│   │   │   ├── utils/            # 工具函数
│   │   │   └── views/           # 页面视图
│   │   ├── Dockerfile
│   │   ├── package.json
│   │   └── vite.config.ts
│   │
│   ├── WO.Property.AuthService/       # 认证服务 (端口5006)
│   │   ├── Controllers/
│   │   ├── Models/
│   │   ├── Services/
│   │   ├── Data/
│   │   └── Program.cs
│   │
│   ├── WO.Property.MaterialService/   # 物料管理 (端口5004)
│   │
│   ├── WO.Property.NotificationService/ # 通知服务 (端口5005)
│   │
│   ├── WO.Property.ContractService/   # 合同管理 (端口5008)
│   │
│   ├── WO.Property.FinanceService/    # 财务管理 (端口5009)
│   │
│   ├── WO.Property.InspectionService/ # 巡检管理 (端口5010)
│   │
│   ├── WO.Property.ComplaintService/  # 投诉建议 (端口5011)
│   │
│   ├── WO.Property.KeyService/        # 钥匙管理 (端口5012)
│   │
│   ├── WO.Property.VisitorService/    # 访客管理 (端口5013)
│   │
│   ├── WO.Property.StatisticsService/ # 统计分析 (端口5014)
│   │
│   ├── WO.Property.MobileService/     # 移动端API (端口5015)
│   │
│   ├── WO.Property.TicketService/    # 工单服务 (端口5002) [开发中]
│   │
│   └── WO.Property.DeviceService/    # 设备服务 (端口5007) [开发中]
│
├── test-login.html               # HTML测试页面
├── vue-test.html                 # Vue测试页面
├── README.md                     # 项目说明
├── PROJECT_PLAN.md               # 项目计划
├── SERVICES_STATUS.md            # 服务状态
└── .gitignore                   # Git忽略配置
```

## 服务端口分配

| 端口 | 服务 | 说明 |
|------|------|------|
| 5003 | DispatchService | 智能派单服务 |
| 5004 | MaterialService | 物料管理 |
| 5005 | NotificationService | 通知服务 |
| 5006 | AuthService | 用户认证 |
| 5007 | DeviceService | 设备服务 [开发中] |
| 5008 | ContractService | 合同管理 |
| 5009 | FinanceService | 财务管理 |
| 5010 | InspectionService | 巡检管理 |
| 5011 | ComplaintService | 投诉建议 |
| 5012 | KeyService | 钥匙管理 |
| 5013 | VisitorService | 访客管理 |
| 5014 | StatisticsService | 统计分析 |
| 5015 | MobileService | 移动端API |
| 5002 | TicketService | 工单服务 [开发中] |

## 模块功能说明

### 核心模块 (已完成)

1. **智能派单服务 (5003)** - 派单规则、任务管理、超时配置
2. **认证服务 (5006)** - 用户登录、权限管理、JWT认证
3. **物料管理 (5004)** - 物料库存、领用记录、供应商管理
4. **通知服务 (5005)** - 系统通知、公告、消息推送
5. **合同管理 (5008)** - 合同创建、付款跟踪、到期提醒
6. **财务管理 (5009)** - 账单生成、收据管理、费用统计
7. **巡检管理 (5010)** - 巡检计划、任务分配、问题记录
8. **投诉建议 (5011)** - 投诉处理、建议收集、满意度调查
9. **钥匙管理 (5012)** - 钥匙借用、归还记录、权限管理
10. **访客管理 (5013)** - 访客预约、通行码、签到签离
11. **统计分析 (5014)** - 数据报表、仪表盘、趋势分析
12. **移动端API (5015)** - 移动端聚合接口、微信小程序支持

### 开发中模块

- **工单服务 (5002)** - 报修工单、进度跟踪
- **设备服务 (5007)** - 设备档案、故障记录

## 运行指南

### 开发环境

```bash
# 启动所有服务
for port in 5003 5004 5005 5006 5008 5009 5010 5011 5012 5013 5014 5015; do
    # 启动各服务
done

# 启动前端
cd src/admin-portal
npm install
npm run dev
```

### Docker部署

```bash
# 构建并启动
docker-compose up -d

# 查看状态
docker-compose ps

# 查看日志
docker-compose logs -f
```

## 测试账号

- **管理员**: admin / Admin@123
- **技术人员**: tech / Tech@123
- **普通用户**: user / User@123

## 访问地址

- **Vue管理后台**: http://localhost:5173 (开发) / http://localhost:3000 (Docker)
- **健康检查**: http://localhost:端口/health

## 开发规范

### 后端 (C#)
- 使用 RESTful API 设计
- 统一 JWT 认证
- 统一错误处理
- 使用枚举替代魔法字符串

### 前端 (Vue 3)
- 使用 TypeScript
- 使用 Composition API
- 组件命名规范 (PascalCase)
- 路由使用命名路由

## 许可证

私有项目 - WO物业管理
