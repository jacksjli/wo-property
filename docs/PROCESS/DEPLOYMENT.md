# 部署文档

> **版本**：v1.0
> **最后更新**：2026-05-05

---

## 1. 部署架构

```
┌─────────────────────────────────────────────────────────────┐
│                         Nginx (反向代理)                      │
│                    端口: 80/443                              │
└────────────────────────────┬────────────────────────────────┘
                             │
          ┌─────────────────┼─────────────────┐
          │                 │                 │
          ▼                 ▼                 ▼
   ┌─────────────┐   ┌─────────────┐   ┌─────────────┐
   │ Frontend    │   │ API Gateway │   │ Admin Panel │
   │  (Vue3)     │   │  (Nginx)    │   │  (Vue3)     │
   │  5173       │   │   5000      │   │   5174      │
   └─────────────┘   └──────┬──────┘   └─────────────┘
                            │
          ┌─────────────────┼─────────────────┐
          │                 │                 │
          ▼                 ▼                 ▼
   ┌─────────────┐   ┌─────────────┐   ┌─────────────┐
   │ AuthService │   │ PersonSvc   │   │ MasterDataSvc│
   │   5006      │   │   5018      │   │   5019       │
   └─────────────┘   └─────────────┘   └─────────────┘
          │                 │                 │
          │                 └────────┬────────┘
          │                          │
          │                          ▼
          │                 ┌─────────────────┐
          │                 │     MySQL       │
          │                 │     3306        │
          │                 └─────────────────┘
          │
          └─────────────────┬─────────────────┐
                            │                 │
                            ▼                 ▼
                    ┌─────────────┐   ┌─────────────┐
                    │ TicketSvc   │   │ DeviceSvc   │
                    │   5002     │   │   5022      │
                    └─────────────┘   └─────────────┘
```

---

## 2. 服务端口

| 服务 | 端口 | 说明 |
|------|------|------|
| API Gateway | 5000 | 统一 API 入口 |
| AuthService | 5006 | 认证服务 |
| TicketService | 5002 | 工单服务 |
| DispatchService | 5003 | 派单服务 |
| PersonService | 5018 | 人员服务 |
| MasterDataService | 5019 | 主数据服务 |
| DeviceService | 5022 | 设备服务 |
| MaterialService | 5023 | 物料服务 |
| ContractService | 5024 | 合同服务 |
| NotificationService | 5028 | 通知服务 |

---

## 3. Docker Compose 部署

### 3.1 docker-compose.yml

```yaml
version: '3.8'

services:
  # MySQL 数据库
  mysql:
    image: mysql:8.0
    container_name: wo_property_mysql
    environment:
      MYSQL_ROOT_PASSWORD: WO_Property_2026
      MYSQL_DATABASE: wo_property
      MYSQL_CHARSET: utf8mb4
      MYSQL_COLLATION: utf8mb4_unicode_ci
    ports:
      - "3306:3306"
    volumes:
      - mysql_data:/var/lib/mysql
    command: --default-auth-plugin=mysql_native_password

  # API Gateway (Nginx)
  gateway:
    image: nginx:alpine
    container_name: wo_property_gateway
    ports:
      - "5000:80"
    volumes:
      - ./nginx/nginx.conf:/etc/nginx/nginx.conf
    depends_on:
      - auth-service
      - person-service
      - masterdata-service

  # 认证服务
  auth-service:
    build: ./services/auth-service
    container_name: wo_property_auth
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__Default=Server=mysql;Database=wo_property;User=root;Password=WO_Property_2026;
    depends_on:
      - mysql

  # 人员服务
  person-service:
    build: ./services/person-service
    container_name: wo_property_person
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__Default=Server=mysql;Database=wo_property;User=root;Password=WO_Property_2026;
    depends_on:
      - mysql

  # 主数据服务
  masterdata-service:
    build: ./services/masterdata-service
    container_name: wo_property_masterdata
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__Default=Server=mysql;Database=wo_property;User=root;Password=WO_Property_2026;
    depends_on:
      - mysql

volumes:
  mysql_data:
```

---

## 4. 环境变量配置

### 4.1 .env 文件

```bash
# 数据库
MYSQL_ROOT_PASSWORD=WO_Property_2026
MYSQL_DATABASE=wo_property

# JWT
JWT_SECRET=YourSuperSecretKeyThatIsAtLeast32CharactersLong!

# 服务端口
GATEWAY_PORT=5000
AUTH_SERVICE_PORT=5006
PERSON_SERVICE_PORT=5018
MASTERDATA_SERVICE_PORT=5019
```

---

## 5. 健康检查

### 5.1 健康检查端点

```
GET /health

响应：
{
  "status": "healthy",
  "service": "TicketService",
  "timestamp": "2026-05-05T08:00:00Z",
  "dependencies": [
    "PersonService: OK",
    "MySQL: OK"
  ]
}
```

### 5.2 Docker 健康检查

```yaml
services:
  person-service:
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5018/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
```

---

## 6. 日志配置

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      { "Name": "Console" },
      { 
        "Name": "File", 
        "Args": { 
          "path": "/var/log/wo-property/app-.log",
          "rollingInterval": "Day"
        }
      }
    ]
  }
}
```

---

## 7. 备份策略

```
每日备份：
  - 全量数据库备份：03:00
  - 备份保留：30天

每周备份：
  - 全量数据库备份：周日 02:00
  - 备份保留：90天

备份存储：
  - 本地：/backup/mysql/
  - 远程：阿里云 OSS
```

---

## 8. 回滚流程

```
1. 发现问题，停止当前部署
   docker-compose down

2. 使用上一个版本的镜像
   docker-compose.yml 中指定旧版本 tag

3. 从备份恢复数据库
   mysql -u root -p wo_property < backup_20260505.sql

4. 重新部署
   docker-compose up -d

5. 验证服务正常
   curl http://localhost:5000/health
```

---

**文档版本**：v1.0
**作者**：运维工程师
**审核**：软件负责人
**状态**：待评审
