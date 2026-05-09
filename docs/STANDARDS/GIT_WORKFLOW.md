# Git 工作流程

> **版本**：v1.0
> **最后更新**：2026-05-05

---

## 1. 分支策略

### 1.1 分支类型

```
main (主分支)
  └── develop (开发分支)
        ├── feature/ticket-123 (功能分支)
        ├── feature/device-api  (功能分支)
        ├── bugfix/ticket-456  (修复分支)
        ├── hotfix/auth-issue   (热修复分支)
        └── release/v1.0.0      (发布分支)
```

### 1.2 分支命名

```
feature/<功能描述>-<Issue号>    # 功能分支
bugfix/<问题描述>-<Issue号>     # 修复分支
hotfix/<问题描述>-<Issue号>     # 热修复分支
release/<版本号>               # 发布分支
```

示例：
```
feature/ticket-status-flow-123
bugfix/login-token-expire-456
hotfix/database-connection-fix
release/v1.0.0
```

---

## 2. 工作流程

### 2.1 功能开发流程

```
1. 从 develop 创建功能分支
   git checkout develop
   git pull origin develop
   git checkout -b feature/ticket-api-123

2. 开发功能，频繁提交
   git add .
   git commit -m "feat(ticket): 添加工单列表API"

3. 保持 develop 最新
   git fetch origin
   git rebase origin/develop

4. 完成开发，推送分支
   git push origin feature/ticket-api-123

5. 创建 Pull Request
   - 指定 Reviewers
   - 关联 Issue
   - 填写 PR 描述

6. Code Review 通过后，合并到 develop
   - 使用 Squash and merge
   - 删除源分支
```

### 2.2 Bug 修复流程

```
1. 从 develop 创建修复分支
   git checkout -b bugfix/ticket-status-bug-456

2. 修复 bug，编写测试
   git commit -m "fix(ticket): 修复状态流转错误"

3. 推送并创建 PR
   git push origin bugfix/ticket-status-bug-456

4. 合并到 develop
```

### 2.3 热修复流程

```
1. 从 main 创建热修复分支
   git checkout main
   git pull origin main
   git checkout -b hotfix/urgent-fix-789

2. 修复并直接合并到 main
   git commit -m "hotfix: 紧急修复登录问题"
   git checkout main
   git merge --no-ff hotfix/urgent-fix-789
   git tag -a v1.0.1 -m "v1.0.1 紧急修复"
   git push origin main --tags

3. 同时合并到 develop
   git checkout develop
   git merge --no-ff hotfix/urgent-fix-789
   git push origin develop
```

---

## 3. Commit 规范

### 3.1 Commit 格式

```
<type>(<scope>): <subject>

<body>

<footer>
```

示例：
```
feat(ticket): 新增工单派单功能

- 实现智能派单规则匹配
- 支持手动指定处理人
- 添加派单回调通知

Closes #123
```

### 3.2 Type 类型

| Type | 说明 |
|------|------|
| feat | 新功能 |
| fix | Bug 修复 |
| docs | 文档更新 |
| style | 代码格式（不影响功能） |
| refactor | 重构 |
| perf | 性能优化 |
| test | 测试相关 |
| chore | 构建/工具 |

---

## 4. Pull Request 规范

### 4.1 PR 描述模板

```markdown
## 概述
[简要描述本次 PR 的变更]

## 变更类型
- [ ] 新功能
- [ ] Bug 修复
- [ ] 重构
- [ ] 文档更新

## 变更内容
- [变更点1]
- [变更点2]

## 影响范围
[本次变更影响的功能模块]

## 测试情况
- [ ] 单元测试通过
- [ ] 集成测试通过
- [ ] 手动测试通过

## 关联 Issue
Closes #123
```

### 4.2 Code Review 检查清单

```
代码质量：
□ 代码符合编码规范
□ 没有重复代码
□ 命名清晰易懂
□ 必要的注释

功能正确性：
□ 功能实现完整
□ 边界条件处理
□ 错误处理完善

测试覆盖：
□ 新功能有测试
□ 修复有回归测试

文档：
□ API 变更有文档
□ 复杂逻辑有注释
```

---

## 5. 版本发布

### 5.1 发布流程

```
1. 创建发布分支
   git checkout -b release/v1.0.0 develop

2. 修复发布版本的问题
   git commit -m "fix: 修复发布版本bug"

3. 合并到 main
   git checkout main
   git merge --no-ff release/v1.0.0
   git tag -a v1.0.0 -m "v1.0.0 正式发布"
   git push origin main --tags

4. 合并回 develop
   git checkout develop
   git merge --no-ff release/v1.0.0
   git push origin develop

5. 删除发布分支
   git branch -d release/v1.0.0
```

### 5.2 版本号规则

```
主版本.次版本.修订号

- 主版本：不兼容的 API 变更
- 次版本：向后兼容的新功能
- 修订号：向后兼容的 bug 修复
```

---

**文档版本**：v1.0
**作者**：运维工程师
**审核**：软件负责人
**状态**：待评审
