# WO 物业管理 - 事件记录 (INCIDENTS)

## 事件-001：Gateway 反复被 watchdog kill（2026-06-02）

**级别**：P2 重复问题
**状态**：✅ 已修复

### 现象
watchdog 每 5 分钟检查服务健康状况时，Gateway 反复被判断为 DOWN → kill -9 → 重启失败 → 循环。

### 根因
watchdog 脚本中使用 `nohup dotnet run ...`，但 `nohup` 在 cron/sudo/systemd 环境里**不继承用户 PATH**，导致找不到 `dotnet` 命令：
```
nohup: dotnet: No such file or directory
```

### 临时修复
手动 kill 后用绝对路径重启：
```bash
/usr/local/share/dotnet/dotnet run -c Release ...
```

### 永久修复
所有脚本中的 `nohup dotnet run` → `nohup /usr/local/share/dotnet/dotnet run`
- `scripts/service-watchdog.sh` — 6 处
- `start-all.sh` — 1 处

### 预防措施
1. **连续崩溃检测**：watchdog 现在会记录每个服务 5 分钟内的崩溃次数，超过 2 次则暂停自动重启，人工介入
2. **回归测试**：`scripts/test-regression.sh` 可验证所有关键服务在重启后正常在线
3. **绝对路径规范**：所有脚本中的外部命令必须使用绝对路径

### 检查清单
- [x] watchdog 已用绝对路径
- [x] start-all.sh 已用绝对路径
- [x] 连续崩溃检测已加入 watchdog
- [x] 回归测试脚本已创建并验证通过
- [x] 本次事件记录到 INCIDENTS.md

---

## 如何添加新事件

```markdown
## 事件-00N：简短描述（日期）

**级别**：P1/P2/P3
**状态**：调查中 / ✅ 已修复

### 现象

### 根因

### 临时修复

### 永久修复

### 预防措施
```

**分级标准**：
- **P1 偶发**：单次出现，未重复 → 修复即可
- **P2 重复**：7 天内 ≥2 次 → 必须 RCA，写入本文档
- **P3 慢性**：30 天内 ≥3 次 → 加监控 + 加自动测试