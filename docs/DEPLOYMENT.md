# WO物业管理软件 - 部署指南

## 目录

1. [环境要求](#环境要求)
2. [开发环境部署](#开发环境部署)
3. [Docker部署](#docker部署)
4. [生产环境配置](#生产环境配置)
5. [常见问题](#常见问题)

---

## 环境要求

### 最低要求

| 组件 | 最低配置 |
|------|----------|
| CPU | 2核 |
| 内存 | 4GB |
| 磁盘 | 20GB |
| 操作系统 | Ubuntu 20.04 / CentOS 8 / macOS 12+ |

### 推荐配置

| 组件 | 推荐配置 |
|------|----------|
| CPU | 4核+ |
| 内存 | 8GB+ |
| 磁盘 | 50GB+ |
| 操作系统 | Ubuntu 22.04 LTS |

### 软件依赖

- .NET 8 SDK
- Node.js 18+
- Docker Engine 24+
- Docker Compose 2.20+

---

## 开发环境部署

### 1. 克隆项目

```bash
git clone <repository-url>
cd WO-Property-Management
```

### 2. 启动后端服务

#### 方式一：分别启动每个服务

```bash
cd src

# 启动认证服务
cd WO.Property.AuthService
dotnet run
# 服务地址: http://localhost:5006

# 新开终端，启动物料服务
cd WO.Property.MaterialService
dotnet run
# 服务地址: http://localhost:5004

# 以此类推...
```

#### 方式二：批量启动脚本

创建 `start-all-services.sh`:

```bash
#!/bin/bash

SERVICES=(
  "WO.Property.AuthService:5006"
  "WO.Property.MaterialService:5004"
  "WO.Property.NotificationService:5005"
  "WO.Property.ContractService:5008"
  "WO.Property.FinanceService:5009"
  "WO.Property.InspectionService:5010"
  "WO.Property.ComplaintService:5011"
  "WO.Property.KeyService:5012"
  "WO.Property.VisitorService:5013"
  "WO.Property.StatisticsService:5014"
  "WO.Property.MobileService:5015"
)

for svc in "${SERVICES[@]}"; do
  name="${svc%%:*}"
  port="${svc##*:}"
  echo "Starting $name on port $port..."
  cd "$name" && nohup dotnet run > "$name.log" 2>&1 &
  cd ..
  sleep 2
done

echo "All services started!"
echo "Check status at:"
for svc in "${SERVICES[@]}"; do
  port="${svc##*:}"
  echo "  http://localhost:$port/health"
done
```

运行:

```bash
chmod +x start-all-services.sh
./start-all-services.sh
```

### 3. 启动前端

```bash
cd src/admin-portal

# 安装依赖
npm install

# 开发模式启动
npm run dev
# 访问地址: http://localhost:5173

# 或构建生产版本
npm run build
# 构建产物在 dist/ 目录
```

### 4. 验证部署

访问各服务健康检查端点:

```bash
for port in 5004 5005 5006 5008 5009 5010 5011 5012 5013 5014 5015; do
  echo -n "Port $port: "
  curl -s http://localhost:$port/health | grep -o '"service":"[^"]*"' || echo "NOT RUNNING"
done
```

预期输出:
```
Port 5004: "service":"MaterialService"
Port 5005: "service":"NotificationService"
Port 5006: "service":"AuthService"
...
```

---

## Docker部署

### 1. 前置准备

确保已安装Docker和Docker Compose:

```bash
docker --version
docker-compose --version
```

### 2. 构建所有服务

#### 构建 .NET 服务

```bash
cd WO-Property-Management

# 构建所有微服务
for svc in WO.Property.AuthService WO.Property.MaterialService \
           WO.Property.NotificationService WO.Property.ContractService \
           WO.Property.FinanceService WO.Property.InspectionService \
           WO.Property.ComplaintService WO.Property.KeyService \
           WO.Property.VisitorService WO.Property.StatisticsService \
           WO.Property.MobileService; do
    echo "Building $svc..."
    dotnet publish src/$svc -c Release -o src/$svc/bin/Release/net8.0/publish
done
```

#### 构建前端

```bash
cd src/admin-portal
npm install
npm run build
```

### 3. 配置环境变量 (可选)

创建 `.env` 文件:

```bash
# 数据库配置
POSTGRES_DB=wo_property
POSTGRES_USER=woproperty
POSTGRES_PASSWORD=YourSecurePassword123!

# JWT配置
JWT_SECRET=YourSecureJWTSecretKey2026ForProduction
JWT_ISSUER=wo-property-unified-auth
JWT_AUDIENCE=wo-property-services

# 环境
ASPNETCORE_ENVIRONMENT=Production
```

### 4. 启动服务

```bash
cd WO-Property-Management

# 启动所有服务 (前台运行)
docker-compose up

# 或后台运行
docker-compose up -d

# 查看状态
docker-compose ps

# 查看日志
docker-compose logs -f

# 查看特定服务日志
docker-compose logs -f auth-service
```

### 5. 访问服务

| 服务 | 地址 |
|------|------|
| 管理后台 | http://localhost:3000 |
| Nginx | http://localhost:80 |
| 认证服务 | http://localhost:5006 |
| API测试 | http://localhost:80/api/auth/health |

### 6. 停止服务

```bash
# 停止所有服务 (保留数据卷)
docker-compose down

# 停止并删除数据卷 (慎用!)
docker-compose down -v

# 完全清理
docker-compose down --rmi all -v
```

---

## 生产环境配置

### 1. Nginx HTTPS配置

创建 `nginx.ssl.conf`:

```nginx
server {
    listen 443 ssl http2;
    server_name your-domain.com;

    ssl_certificate /etc/nginx/ssl/certificate.crt;
    ssl_certificate_key /etc/nginx/ssl/private.key;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;

    # 其他配置同 docker/nginx.conf
}

server {
    listen 80;
    server_name your-domain.com;
    return 301 https://$server_name$request_uri;
}
```

### 2. PostgreSQL生产配置

```bash
# 创建生产数据库
docker exec -it wo-property-postgres psql -U woproperty -d wo_property

# 在psql中执行
CREATE DATABASE wo_property_production;
ALTER DATABASE wo_property_production SET timezone TO 'Asia/Shanghai';
```

### 3. 数据备份

#### 备份脚本 `backup.sh`:

```bash
#!/bin/bash
DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_DIR=/backups/wo-property
mkdir -p $BACKUP_DIR

# 备份数据库
docker exec wo-property-postgres pg_dump -U woproperty wo_property > $BACKUP_DIR/db_$DATE.sql

# 备份配置文件
tar -czf $BACKUP_DIR/config_$DATE.tar.gz docker/

# 保留最近30天备份
find $BACKUP_DIR -mtime +30 -delete

echo "Backup completed: $DATE"
```

### 4. 监控配置

推荐使用:
- **Prometheus** - 指标收集
- **Grafana** - 可视化
- **Loki** - 日志收集
- **AlertManager** - 告警

### 5. 服务更新

```bash
# 拉取最新代码
git pull

# 重新构建
for svc in WO.Property.*; do
    dotnet publish src/$svc -c Release -o src/$svc/bin/Release/net8.0/publish
done

cd src/admin-portal && npm run build && cd ../..

# 重启服务
docker-compose down
docker-compose up -d

# 查看更新后的版本
docker-compose exec auth-service dotnet --version
```

---

## 常见问题

### Q1: 服务启动失败，端口被占用

```bash
# 查看端口占用
lsof -i :5006

# 或
netstat -tlnp | grep 5006

# 杀死占用进程
kill -9 <PID>
```

### Q2: 数据库连接失败

```bash
# 检查PostgreSQL容器状态
docker-compose ps postgres

# 查看日志
docker-compose logs postgres

# 重启数据库
docker-compose restart postgres
```

### Q3: 前端无法连接后端API

1. 检查后端服务是否运行
2. 检查Nginx代理配置
3. 检查CORS设置
4. 检查JWT Token是否有效

### Q4: Docker构建失败

```bash
# 清理Docker缓存
docker system prune -a

# 重新构建
docker-compose build --no-cache
```

### Q5: 内存不足

```bash
# 增加Docker内存限制
# Docker Desktop -> Settings -> Resources -> Memory: 8GB+
```

### Q6: HTTPS证书问题

```bash
# 使用Let's Encrypt免费证书
certbot --nginx -d your-domain.com

# 或使用自签名证书 (测试环境)
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout private.key -out certificate.crt
```

---

## 联系支持

如有问题，请联系开发团队。
