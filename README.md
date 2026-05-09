# WO物业管理软件

> 一套基于 .NET 8 微服务架构的现代化物业管理平台

## 📋 项目简介

WO物业管理软件是一套功能完善的物业管理解决方案，采用前后端分离架构，基于微服务设计，支持多端访问和容器化部署。

## ✨ 核心特性

- **微服务架构** - 11个独立部署的业务服务
- **统一身份认证** - JWT令牌，支持SSO
- **前后端分离** - Vue 3 + Element Plus
- **容器化部署** - Docker Compose一键部署
- **多端支持** - Web管理后台、移动端API、微信小程序

## 🏗️ 技术栈

### 后端
- .NET 8 LTS
- ASP.NET Core Web API
- Entity Framework Core
- SQLite (开发) / PostgreSQL (生产)
- JWT Bearer Authentication

### 前端
- Vue 3 + Composition API
- TypeScript
- Element Plus
- Vite
- Pinia + Vue Router

### 基础设施
- Docker + Docker Compose
- Nginx Reverse Proxy
- Redis (可选缓存)

## 📦 模块列表

| 模块 | 端口 | 功能 |
|------|------|------|
| 认证服务 | 5006 | 用户登录、权限、JWT |
| 物料管理 | 5004 | 物料库存、领用、供应商 |
| 通知服务 | 5005 | 系统通知、公告推送 |
| 合同管理 | 5008 | 合同创建、付款跟踪 |
| 财务管理 | 5009 | 账单生成、收据管理 |
| 巡检管理 | 5010 | 巡检计划、任务分配 |
| 投诉建议 | 5011 | 投诉处理、满意度调查 |
| 钥匙管理 | 5012 | 钥匙借用、归还记录 |
| 访客管理 | 5013 | 访客预约、通行码 |
| 统计分析 | 5014 | 数据报表、仪表盘 |
| 移动端API | 5015 | 聚合接口、微信支持 |

## 🚀 快速开始

### 环境要求

- .NET 8 SDK
- Node.js 18+
- Docker (可选)

### 启动后端服务

```bash
cd src

# 启动认证服务
cd WO.Property.AuthService
dotnet run
# 访问: http://localhost:5006

# 其他服务同理，端口见上表
```

### 启动前端

```bash
cd src/admin-portal
npm install
npm run dev
# 访问: http://localhost:5173
```

### Docker部署

```bash
# 启动所有服务
docker-compose up -d

# 查看状态
docker-compose ps

# 查看日志
docker-compose logs -f
```

## 🔐 测试账号

| 角色 | 用户名 | 密码 |
|------|--------|------|
| 管理员 | admin | Admin@123 |
| 技术人员 | tech | Tech@123 |
| 普通用户 | user | User@123 |

## 📂 项目结构

```
WO-Property-Management/
├── docker/                      # Docker配置
├── src/
│   ├── admin-portal/          # Vue管理后台
│   └── WO.Property.*/         # 11个微服务
├── docker-compose.yml
├── README.md
└── PROJECT_STRUCTURE.md       # 详细结构文档
```

## 📚 更多文档

- [项目结构详解](PROJECT_STRUCTURE.md)
- [服务状态](SERVICES_STATUS.md)
- [JWT配置说明](JWT_CONFIG.md)
- [部署指南](docker/README.md)

## 📄 API文档

所有API采用RESTful设计，基础URL格式：

```
http://localhost:{端口}/api/{模块}/{资源}
```

认证方式：所有API需要在Header中携带JWT Token：

```
Authorization: Bearer {token}
```

## 🐛 常见问题

**Q: 服务无法启动？**
A: 检查端口是否被占用，数据库文件是否存在。

**Q: 登录失败？**
A: 确认使用正确的测试账号，检查认证服务是否正常运行。

**Q: 前端页面空白？**
A: 检查浏览器控制台错误，确保后端API服务全部运行。

## 📧 联系方式

私有项目 - 如有问题请联系开发团队

## 📄 许可证

私有项目 - WO物业管理 © 2026
