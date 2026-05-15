# CI/CD 编译前规范检查

## 状态：✅ 已完成

## CI 体系架构

```
本地开发:  bash scripts/local-ci.sh         # 本地验证（预检 + 编译）
GitHub:    .github/workflows/ci.yml        # Push/PR 时自动触发
```

---

## 触发时机

| 环境 | 触发条件 |
|------|----------|
| **本地** | `bash scripts/local-ci.sh` — 手动或 pre-commit hook |
| **GitHub Actions** | `git push` / `PR` / 手动 `workflow_dispatch` |

---

## GitHub Actions CI 流程

```
push/PR
    │
    ▼
┌─────────────────┐
│  pre-check job │  ← 规范检查（必须通过才能继续）
└────────┬────────┘
         │ success
         ▼
    ┌────────────┐     ┌─────────────────┐
    │ backend-   │     │ frontend-build  │
    │ build (21) │     │ job            │
    └─────┬──────┘     └────────┬────────┘
          │                    │
          └────────┬───────────┘
                   │ all success
                   ▼
            ┌───────────┐
            │ ci-       │
            │ complete   │
            └───────────┘
```

### Job 1: 预检（pre-check）
```bash
bash scripts/pre-build-check.sh
```
检查项：
1. UseMySql block-style（禁止单行嵌套）
2. 数据库 Seeding（禁止启动时查表）
3. 前端 API 直连端口（必须走 Gateway）
4. app.Run 硬编码端口

### Job 2: 后端编译（backend-build）
- 21 个服务并行编译
- 任意一个失败 → CI 失败
- 编译后上传 DLL artifacts（保留 3 天）

### Job 3: 前端编译（frontend-build）
- admin-portal `npm run build`
- 编译后上传 dist artifacts（保留 3 天）

### Job 4: 汇总（ci-complete）
- 所有 job 必须 success 才算通过
- 任意 job failure → 最终结果 failure

---

## 本地 CI 脚本

```bash
# 快速检查（仅规范）
bash scripts/pre-build-check.sh

# 完整检查（规范 + 编译）
bash scripts/local-ci.sh
```

---

## 规范检查规则

| # | 检查项 | 违规级别 | 处理 |
|---|--------|----------|------|
| 1 | UseMySql 单行嵌套 | P1 | 编译失败 |
| 2 | 数据库 Seeding 查表 | P1 | 编译失败 |
| 3 | 前端直连服务端口 | P1 | 编译失败 |
| 4 | app.Run 硬编码端口 | P1 | 编译失败 |

---

## 当前违规状态

✅ **所有违规已修复**（截至 2026-05-15 08:55）

| 检查项 | 状态 |
|--------|------|
| UseMySql block-style | ✅ 通过 |
| 数据库 Seeding | ✅ 无违规 |
| 前端 API 直连 | ✅ 无违规 |
| app.Run 硬编码 | ✅ 无违规 |

---

## 下一步

**GitHub Actions 需要：**
1. 将代码推送到 GitHub 仓库
2. 在 GitHub repo 设置 → Actions → 启用 Workflow
3. （可选）添加 `workflow_run` 限制只在 main 分支成功时才合并

```bash
git add .github/ scripts/local-ci.sh scripts/pre-build-check.sh
git commit -m "ci: add GitHub Actions CI workflow"
git remote add origin https://github.com/YOUR_ORG/WO-Property-Management.git
git push origin main
```