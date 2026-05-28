# WO 物业管理软件 - Docker 部署指南

## 快速部署

```bash
# 1. 进入部署目录
cd deploy

# 2. 复制环境配置
cp .env.example .env

# 3. 编辑 .env 文件，修改密码
vim .env

# 4. 启动所有服务
docker-compose up -d

# 5. 查看服务状态
docker-compose ps

# 6. 查看日志
docker-compose logs -f
```

## 服务访问

| 服务 | 地址 |
|------|------|
| 管理后台 | http://服务器IP:5173 |
| Gateway API | http://服务器IP:5000 |
| 小程序API | http://服务器IP:3000 |

## 停止服务

```bash
docker-compose down
```

## 重新构建

```bash
docker-compose build --no-cache
docker-compose up -d
```

## 数据库初始化

首次启动会自动执行 `init.sql` 初始化数据库。

## 注意事项

1. 确保服务器 3306, 5000, 5173, 3000 端口未被占用
2. 正式环境请修改 `.env` 中的默认密码
3. 如需 HTTPS，请配置 Nginx 反向代理