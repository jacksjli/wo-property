# WO Property Management System - Docker 容器化部署文档

## 📋 概述

本文档描述 WO 物业管理软件基于 Docker Compose 的容器化部署方案，支持开发者通过 `docker-compose up` 快速启动完整系统。

**特性：**
- 多阶段构建 .NET 8 微服务镜像
- MySQL 8.0（utf8mb4 字符集）
- Vue3 前端容器内构建（Nginx 托管）
- Nginx 反向代理（静态资源缓存 + API 路由）
- 非 root 用户运行
- 健康检查覆盖所有服务
- 数据卷持久化

---

## 🏗️ 系统架构

```
                                    ┌─────────────────────────────────────────┐
                                    │              nginx (alpine)              │
Internet ──── :80/:443 ───────────►│  静态资源 (1y缓存)  │  API → Gateway  │
                                    └───────────────┬─────────────────────────┘
                                                    │
                         ┌──────────────────────────┼──────────────────────────┐
                         │                          │                          │
                         ▼                          ▼                          ▼
                  ┌────────────┐           ┌─────────────┐            ┌──────────────┐
                  │  frontend  │           │   gateway   │            │ auth-service │
                  │ (Vue3/Nginx)│           │  (5000/Ocelot)│          │  (5006)       │
                  │   :3000    │           └──────┬──────┘            └──────────────┘
                  └────────────┘                  │
                         │                        │ (YARP 反向代理到各微服务)
                         │                        ▼
                         │        ┌──────────────────────┐
                         │        │   masterdata-service │
                         │        │      (5019)          │
                         │        ├──────────────────────┤
                         │        │   person-service     │
                         │        │      (5018)          │
                         │        ├──────────────────────┤
                         │        │   ticket-service      │
                         │        │      (5002)           │
                         │        └──────────────────────┘
                         │
┌─────────────────────────┴──────────────────────┐
│              wo-network (bridge)                │
│                                                │
│   ┌──────────────┐     ┌────────────────────┐  │
│   │    mysql     │     │  各微服务容器      │  │
│   │  (3306)      │     │  (bridge network) │  │
│   │  wo_property │     └────────────────────┘  │
│   └──────────────┘                               │
└─────────────────────────────────────────────────┘
```

---

## 📁 目录结构

```
WO-Property-Management/
├── docker/
│   ├── nginx.prod.conf       # 生产级 Nginx 配置
│   ├── Dockerfile.dotnet      # .NET 服务 Dockerfile 模板
│   └── ssl/                  # SSL 证书（自签或真实）
├── docker-compose.yml         # 完整开发/生产环境配置
├── docker-compose.yml.bak     # 原配置备份
├── scripts/
│   ├── docker-up.sh          # 首次启动脚本
│   ├── docker-down.sh        # 停止所有容器脚本
│   └── deploy.sh             # 部署脚本（重启/重建）
├── infrastructure/
│   └── mysql/
│       └── init/
│           └── 01-init.sql   # MySQL 初始化脚本
├── src/
│   ├── WO.Property.GatewayService/   (Dockerfile 已存在)
│   ├── WO.Property.AuthService/       (Dockerfile 已存在)
│   ├── WO.Property.MasterDataService/(需补充 Dockerfile)
│   ├── WO.Property.PersonService/     (新增 Dockerfile)
│   ├── WO.Property.TicketService/     (新增 Dockerfile)
│   └── admin-portal/                  (Dockerfile 已存在)
└── docs/
    └── PRODUCTION_DOCKER.md          # 本文档
```

---

## 🚀 快速开始

### 首次启动

```bash
cd WO-Property-Management

# 方式1：使用启动脚本（推荐）
./scripts/docker-up.sh --build

# 方式2：直接使用 docker-compose
docker compose build --no-cache
docker compose up -d
```

### 停止服务

```bash
# 停止（保留数据）
./scripts/docker-down.sh

# 停止并清理镜像（节省空间）
./scripts/docker-down.sh --clean

# 停止并删除数据卷（慎用！）
./scripts/docker-down.sh --volumes
```

### 部署更新

```bash
# 重启所有服务（使用已有镜像）
./scripts/deploy.sh

# 重新构建并重启所有服务
./scripts/deploy.sh --build

# 仅重启指定服务
./scripts/deploy.sh gateway
./scripts/deploy.sh auth-service --build
```

---

## 📦 服务清单

| 服务 | 容器名 | 端口 | 说明 |
|------|--------|------|------|
| `mysql` | `wo-property-mysql` | 3306 | MySQL 8.0, utf8mb4 |
| `gateway` | `wo-gateway` | 5000 | API 网关 (YARP) |
| `auth-service` | `wo-auth-service` | 5006 | 认证服务 |
| `masterdata-service` | `wo-masterdata-service` | 5019 | 主数据服务 |
| `person-service` | `wo-person-service` | 5018 | 人员服务 |
| `ticket-service` | `wo-ticket-service` | 5002 | 工单服务 |
| `nginx` | `wo-property-nginx` | 80/443 | 反向代理 + 前端托管 |
| `frontend` | `wo-frontend` | 3000 | Vue3 前端 |

---

## 🐳 Dockerfile 说明

所有 .NET 服务使用**统一的多阶段构建**模式：

```dockerfile
# 阶段1: 构建
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# 阶段2: 运行
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
RUN apt-get install curl          # 健康检查依赖
RUN groupadd/appgroup + useradd  # 非root用户
COPY --from=build /app/publish .
USER appuser
EXPOSE <PORT>
ENV ASPNETCORE_URLS=http://+:PORT
HEALTHCHECK --interval=30s CMD curl -f localhost:PORT/health
ENTRYPOINT ["dotnet", "*.dll"]
```

**关键优化点：**
- ✅ 多阶段构建（构建SDK ⊄ 运行镜像）
- ✅ 非 root 用户（appuser:appgroup, uid 1000）
- ✅ dotnet publish（无 --no-restore）
- ✅ 健康检查（curl → /health）
- ✅ 资源限制（deploy.resources.limits）

---

## 🌐 Nginx 配置说明

`docker/nginx.prod.conf` 核心配置：

| 功能 | 配置 |
|------|------|
| 静态资源缓存 | `expires 1y`（JS/CSS/字体） |
| API 反向代理 | `/api/` → `gateway:5000` |
| Gzip 压缩 | `gzip_comp_level 6` |
| 安全头 | X-Frame-Options, X-Content-Type-Options, CSP |
| 上游超时 | `proxy_read_timeout 60s` |
| WebSocket | `Upgrade $http_upgrade` |

**缓存策略：**
- `*.js, *.css, *.woff2` → `expires 1y`（版本化文件名长期缓存）
- `*.png, *.jpg, ...` → `expires 30d`
- `*.html` → `no-store, no-cache`（不缓存）

---

## 🗄️ 数据库

**MySQL 配置：**
- 镜像：`mysql:8.0`
- 字符集：`utf8mb4_unicode_ci`
- 初始化脚本：`infrastructure/mysql/init/01-init.sql`
- 数据卷：`mysql_data:/var/lib/mysql`

**数据迁移：**
首次启动后，需手动执行数据库迁移：

```bash
# 查看迁移文件
ls migrations/*.sql

# 进入 MySQL 容器执行迁移
docker exec -i wo-property-mysql mysql -u woproperty -pWOProperty2026! wo_property < migrations/masterdata_service_v1_init.sql
docker exec -i wo-property-mysql mysql -u woproperty -pWOProperty2026! wo_property < migrations/0035_RenovationRequests.sql
# ... 其他迁移文件
```

> ⚠️ **注意**：MySQL 8.0 的 `docker-entrypoint-initdb.d` 脚本仅在**数据卷为空时**首次启动执行。后续启动不会重复执行。如需重新初始化，必须先删除数据卷：`docker volume rm wo-property-mysql_mysql_data`

---

## 🔧 运维命令

```bash
# 查看所有服务状态
docker compose ps

# 查看所有日志
docker compose logs -f

# 查看指定服务日志
docker compose logs -f gateway
docker compose logs -f auth-service

# 进入容器
docker exec -it wo-property-mysql mysql -u woproperty -pWOProperty2026! wo_property
docker exec -it wo-gateway sh

# 检查服务健康
docker compose ps

# 重启指定服务
docker compose restart gateway

# 构建指定服务
docker compose build auth-service

# 查看资源使用
docker stats
```

---

## 🔐 环境变量

所有服务通过 `docker-compose.yml` 的 `environment` 注入配置：

| 变量 | 示例值 | 说明 |
|------|--------|------|
| `DB_CONNECTION_STRING` | `Server=mysql;Port=3306;...` | MySQL 连接字符串 |
| `JWT_SECRET_KEY` | `WO-Property-...` | JWT 签名密钥 |
| `JWT_ISSUER` | `wo-property-unified-auth` | JWT 发行方 |
| `JWT_AUDIENCE` | `wo-property-services` | JWT 接收方 |
| `ASPNETCORE_ENVIRONMENT` | `Production` | 运行环境 |

---

## ⚠️ 注意事项

1. **端口冲突**：确保本机 3306/5000/5006/5018/5019/80/443 未被占用
2. **首次启动慢**：首次构建 .NET 镜像需下载 SDK（约 700MB），耐心等待
3. **数据库迁移**：服务启动后需手动执行 `migrations/*.sql`
4. **Windows Docker Desktop**：建议分配 4GB+ 内存和 2+ CPU cores
5. **SSL 证书**：生产环境需替换 `docker/ssl/` 下的自签证书
6. **日志收集**：`nginx_logs` 卷需定期清理：`docker volume rm wo-property-nginx_logs`

---

## 📊 健康检查矩阵

| 服务 | 健康检查端点 | 超时 |
|------|-------------|------|
| mysql | `mysqladmin ping` | 5s |
| gateway | `curl localhost:5000/health` | 3s |
| auth-service | `curl localhost:5006/health` | 3s |
| masterdata-service | `curl localhost:5019/health` | 3s |
| person-service | `curl localhost:5018/health` | 3s |
| ticket-service | `curl localhost:5002/health` | 3s |
| nginx | `curl localhost/health` | 10s |
| frontend | `wget localhost/health` | 10s |

---

_文档版本：1.0.0_
_创建日期：2026-05-10_
_适用版本：WO Property Management v1.0+_
