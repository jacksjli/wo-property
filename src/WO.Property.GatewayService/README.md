# WO.Property.GatewayService - API 网关

## 功能特性

- **YARP 智能路由** - 根据路径自动路由到对应下游服务
- **JWT 统一认证** - 所有服务共享统一 JWT 配置，无需各自验证
- **下游服务熔断** - 使用 Polly 实现熔断 + 指数退避重试
- **全局限流** - 基于令牌桶算法，按用户/IP 限流
- **项目隔离 Header** - 自动注入 `X-Project-Code` 到下游服务
- **统一 Swagger 文档** - 一个入口看到所有下游 API

## 启动

```bash
cd WO.Property.GatewayService
dotnet run
# 访问 http://localhost:5000/swagger
```

## 路由配置

路由配置在 `appsettings.json` 的 `YARP.Routes` 节点：

```json
"YARP": {
  "Routes": {
    "auth-route": {
      "ClusterId": "auth-cluster",
      "Match": { "Path": "/api/auth/{**catch-all}" }
    }
  },
  "Clusters": {
    "auth-cluster": {
      "Destinations": {
        "auth-1": { "Address": "http://localhost:5006" }
      }
    }
  }
}
```

## 生产部署

1. 复制 `config.yaml.example` 为 `config.yaml`
2. 填入生产 JWT Secret（强随机密钥，至少 32 字符）
3. 配置正确的 `AllowedOrigins`
4. 配置下游服务地址（Cluster Destinations）
5. 使用 Nginx 反向代理到 5000 端口
