# WO物业管理系统 - Docker部署说明

## 快速开始

### 1. 构建所有镜像

```bash
cd ~/Projects/WO-Property-Management

# 构建所有 .NET 服务
for svc in WO.Property.AuthService WO.Property.MaterialService WO.Property.NotificationService \
           WO.Property.ContractService WO.Property.FinanceService WO.Property.InspectionService \
           WO.Property.ComplaintService WO.Property.KeyService WO.Property.VisitorService \
           WO.Property.StatisticsService WO.Property.MobileService; do
    dotnet publish src/$svc -c Release -o src/$svc/bin/Release/net8.0/publish
done

# 构建前端 (需要在 src/admin-portal 目录运行 npm run build)
```

### 2. 启动所有服务

```bash
docker-compose up -d
```

### 3. 查看服务状态

```bash
docker-compose ps
```

### 4. 查看日志

```bash
docker-compose logs -f
```

## 服务端口

| 服务 | 端口 | 说明 |
|------|------|------|
| PostgreSQL | 5432 | 数据库 |
| Redis | 6379 | 缓存 |
| Nginx | 80 | 反向代理 |
| 认证服务 | 5006 | 用户认证 |
| 物料服务 | 5004 | 物料管理 |
| 通知服务 | 5005 | 通知推送 |
| 合同服务 | 5008 | 合同管理 |
| 财务服务 | 5009 | 财务管理 |
| 巡检服务 | 5010 | 巡检管理 |
| 投诉服务 | 5011 | 投诉建议 |
| 钥匙服务 | 5012 | 钥匙管理 |
| 访客服务 | 5013 | 访客管理 |
| 统计服务 | 5014 | 统计分析 |
| 移动服务 | 5015 | 移动端API |
| Vue前端 | 3000 | 管理后台 |

## 访问地址

- **管理后台**: http://localhost:3000
- **API网关**: http://localhost/api/*
- **健康检查**: http://localhost/health

## 停止服务

```bash
docker-compose down
```

## 清理数据

```bash
docker-compose down -v
```

## 生产环境建议

1. **使用环境变量**: 在生产环境中使用 .env 文件管理敏感信息
2. **SSL证书**: 在生产环境中启用 HTTPS
3. **数据卷**: 定期备份 PostgreSQL 和 Redis 数据卷
4. **日志管理**: 配置日志轮转
5. **监控**: 添加 Prometheus 和 Grafana 监控

## 开发环境 vs 生产环境

- **开发环境**: 使用 SQLite，直接运行 dotnet run
- **生产环境**: 使用 PostgreSQL，通过 Docker Compose 部署
