# WO-Property CI/CD 流水线文档

> 维护人：DevOps 工程师  
> 最后更新：2026-05-10  
> 适用版本：v1.0+

---

## 1. 概览

```
feature/* ──→ PR ──→ CI ──────────────────────────→ merge to main
                          │                                   │
                          ├── ✅ 测试通过                      │
                          ├── 🐳 镜像构建+推送                  │
                          │   (ghcr.io/username/woproperty/*) │
                          └── 🚀 自动部署测试环境               │
                                                        │
                           develop branch: 自动部署测试环境     │
                           main branch:   等待手动 CD 审批     │
```

---

## 2. 分支策略

| 分支 | 用途 | CI 触发 | CD 触发 |
|------|------|---------|---------|
| `feature/*` | 功能开发 | ✅ PR 时触发 | ❌ |
| `develop` | 开发集成 | ✅ Push / PR | ✅ 自动部署测试环境 |
| `main` | 稳定版本 | ✅ Push / PR | ✅ 手动触发（需审批） |

**PR 合并规则：** 所有 PR 必须通过 CI（测试 + 构建）才能合并到 `main`。

---

## 3. GitHub Actions 工作流

### 3.1 CI Pipeline (`ci.yml`)

| Job | 触发条件 | 内容 |
|-----|----------|------|
| `backend-build` | 每次 Push/PR | .NET 8 服务：restore → build → test（5个服务并行） |
| `frontend-build` | 每次 Push/PR | Vue3 admin-portal：install → type-check → lint → build |
| `docker-build-and-push` | Push to main/develop 或手动 | 构建 + 推送镜像到 GHCR |
| `deploy-test` | develop 分支或手动 `deploy_test=true` | 部署到测试服务器 |

**Docker 镜像 Tag 策略：**
```
ghcr.io/{owner}/woproperty/{service}:{branch}-{sha}
ghcr.io/{owner}/woproperty/{service}:latest          # 仅 main 分支
示例：
  ghcr.io/xxx/woproperty/gateway:main-abc1234
  ghcr.io/xxx/woproperty/auth-service:develop-def5678
  ghcr.io/xxx/woproperty/frontend:latest
```

### 3.2 CD Deploy (`deploy.yml`)

- **触发方式：** GitHub Actions UI → `workflow_dispatch`
- **参数：**
  - `environment` — test / staging / prod（默认 prod）
  - `service` — all / gateway / auth-service / ...（默认 all）
  - `version` — 指定镜像版本 SHA（留空则使用 `main-{sha}`）
- **流程：** 审批门 → SSH 远程服务器 → 拉取镜像 → 重启服务 → 健康检查 → 失败自动回滚
- **回滚：** 读取最近一次 `docker-compose.yml.backup.*` 恢复

### 3.3 CD Trigger (`cd-trigger.yml`)

- **用途：** 人工触发发布申请单，发送钉钉/企微通知，然后触发 `deploy.yml`
- **适合场景：** 需要记录"谁在什么时间因为什么发布了"的团队

---

## 4. GitHub Secrets 配置清单

在 GitHub → Settings → Secrets and variables → Actions 中配置：

| Secret 名称 | 说明 | 示例值 |
|-------------|------|--------|
| `TEST_SERVER_HOST` | 测试服务器 IP/域名 | `test.wo-property.example.com` |
| `TEST_SERVER_USER` | 测试服务器 SSH 用户 | `deploy` |
| `TEST_SERVER_SSH_KEY` | 测试服务器私钥 | `-----BEGIN OPENSSH...` |
| `PROD_SERVER_HOST` | 生产服务器 IP/域名 | `wo-property.example.com` |
| `PROD_SERVER_USER` | 生产服务器 SSH 用户 | `deploy` |
| `PROD_SERVER_SSH_KEY` | 生产服务器私钥 | `-----BEGIN OPENSSH...` |
| `PROD_SERVER_PORT` | SSH 端口 | `22` |
| `DINGTALK_WEBHOOK` | 钉钉机器人 Webhook | `https://oapi.dingtalk.com/...` |
| `WECOM_WEBHOOK` | 企微机器人 Webhook | `https://qyapi.weixin.qq.com/...` |

---

## 5. 环境变量模板 (`.env.example`)

```bash
# 复制到服务器上作为模板
cp .env.example /opt/wo-property/.env

# 填入真实值（不要提交到 Git）
# 所有敏感值应通过 GitHub Secrets 或服务器本地环境变量注入
```

---

## 6. 生产服务器目录结构

```
/opt/wo-property/
├── docker-compose.yml          # 主配置（已在服务器上）
├── docker-compose.prod.yml      # CD 生成的 override（覆盖镜像版本）
├── docker-compose.yml.backup.*  # 自动备份
├── scripts/
│   └── cicd/
│       └── deploy.sh           # CD 部署脚本
└── (其他运行时数据卷)
```

---

## 7. 部署流程（生产环境）

```
① 开发人员 → GitHub PR → 自动 CI
② CI 通过 → 代码审查 + 合并到 main
③ Release Manager → GitHub Actions → 手动触发 CD Trigger
④ CD Trigger → 钉钉通知审批人 → 触发 deploy.yml
⑤ GitHub Environment Protection → 要求审批人批准
⑥ 审批通过 → SSH 部署 → 健康检查
⑦ 检查失败 → 自动回滚 → 通知失败
⑧ 检查成功 → 通知成功
```

---

## 8. 服务列表 & 端口

| 服务 | 镜像名称 | 端口 |
|------|----------|------|
| Gateway | `gateway` | 5000 |
| Auth Service | `auth-service` | 5006 |
| MasterData Service | `masterdata-service` | 5019 |
| Person Service | `person-service` | 5018 |
| Ticket Service | `ticket-service` | 5002 |
| Frontend (admin-portal) | `frontend` | 80/3000 |

---

## 9. 健康检查端点

| 服务 | 端点 |
|------|------|
| Nginx | `GET /health` |
| Gateway | `GET /health` |
| Auth Service | `GET /health` |
| MasterData Service | `GET /health` |
| Person Service | `GET /health` |
| Ticket Service | `GET /health` |

---

## 10. 常见问题

### Q1: CI 测试失败怎么办？
检查 `Actions` → 对应 Run → `test-results-{service}` artifact 下载 `.trx` 文件查看详细错误。

### Q2: 镜像拉取失败？
确认 GitHub Packages 访问权限：`Settings → Packages → Public`，确保 `read` 权限已开启。

### Q3: 部署后健康检查超时？
1. 确认服务器网络可达（防火墙/安全组）
2. 检查容器日志：`docker compose logs -f [service]`
3. 确认 `.env` 文件中数据库连接字符串正确

### Q4: 如何回滚到指定版本？
在 GitHub Actions → `CD - Production Deploy` → `Run workflow` 中手动输入要回滚的 `version`（如 `main-abc1234`）。

---

## 11. 相关文件清单

```
WO-Property-Management/
├── .github/
│   └── workflows/
│       ├── ci.yml           # CI 流水线
│       ├── deploy.yml       # CD 生产部署
│       └── cd-trigger.yml   # CD 手动审批流
├── .env.example             # 环境变量模板
├── scripts/
│   └── cicd/
│       └── deploy.sh        # 服务器端部署脚本
└── docs/
    └── PRODUCTION_CICD.md  # 本文档
```
