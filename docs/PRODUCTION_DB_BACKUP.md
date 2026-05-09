# WO-Property 物业管理软件 — MySQL 主从备份与灾难恢复方案

> 版本：v1.0  
> 作者：运维工程师（subagent）  
> 日期：2026-05-09  
> 状态：设计稿，待评审

---

## 目录

- [1. 概述与目标](#1-概述与目标)
- [2. MySQL 主从复制方案](#2-mysql-主从复制方案)
- [3. 备份策略设计](#3-备份策略设计)
- [4. 灾难恢复预案](#4-灾难恢复预案)
- [5. 监控告警设计](#5-监控告警设计)
- [6. 脚本清单](#6-脚本清单)
- [7. 实施路线图](#7-实施路线图)

---

## 1. 概述与目标

### 1.1 当前状态

| 项目 | 现状 |
|------|------|
| 数据库 | MySQL（库名 `wo_property`） |
| 数据量 | 约 3.4 GB（blob/mediumtext 字段为主） |
| 端口 | 3306 |
| 备份机制 | **无任何备份** |
| 运行环境 | macOS 开发机（本地）+ Linux 生产服务器 |

### 1.2 恢复目标

| 指标 | 目标值 | 说明 |
|------|--------|------|
| **RPO** | ≤ 1 小时 | 数据丢失容忍最多 1 小时 |
| **RTO** | ≤ 4 小时 | 服务中断后 4 小时内恢复 |

---

## 2. MySQL 主从复制方案

### 2.1 架构概览

```
┌─────────────────┐      binlog 复制       ┌─────────────────┐
│   主库 (Master)  │ ──────────────────→  │   从库 (Slave)   │
│  192.168.1.100   │                      │  192.168.1.101   │
│  端口: 3306      │                      │  端口: 3306      │
└─────────────────┘                      └─────────────────┘
        │                                      │
        ▼                                      ▼
  [备份文件]                            [延迟复制 1 小时]
```

### 2.2 主库配置（macOS 本地 / Linux 生产通用）

**文件：** `/usr/local/etc/my.cnf`（macOS Homebrew）或 `/etc/mysql/my.cnf`（Linux）

```ini
[mysqld]
# === 基本配置 ===
server-id = 1
port = 3306
datadir = /usr/local/var/mysql   # macOS
# datadir = /var/lib/mysql       # Linux

# === Binlog 配置 ===
log-bin = mysql-bin
binlog_format = ROW               # 行复制，最安全
binlog_rows_query_log_events = ON
max_binlog_size = 100M           # 单个 binlog 文件最大 100MB
binlog_expire_logs_seconds = 604800  # binlog 保留 7 天（主从延迟最多 7 天内可追）
sync_binlog = 1                  # 每次事务提交都同步 binlog 到磁盘（最安全，但略慢）

# === GTID 模式（推荐）===
gtid_mode = ON
enforce_gtid_consistency = ON

# === 安全 ===
read_only = OFF                 # 主库可读写
super_read_only = OFF

# === 性能 ===
innodb_flush_log_at_trx_commit = 1
innodb_buffer_pool_size = 1G    # 根据实际内存调整
max_connections = 200

# === 日志 ===
slow_query_log = 1
slow_query_log_file = /var/log/mysql/slow.log
long_query_time = 2
```

> **注：** 修改配置后执行 `mysqladmin reload` 或重启 MySQL。

### 2.3 从库配置

**文件：** `/usr/local/etc/my.cnf.d/replica.cnf`（Linux）或通过 Homebrew 安装目录配置

```ini
[mysqld]
# === 基本配置 ===
server-id = 2                   # 必须与主库不同
port = 3306
datadir = /var/lib/mysql

# === Binlog（从库也建议开启，必要时可升为主）===
log-bin = mysql-bin
binlog_format = ROW
max_binlog_size = 100M
binlog_expire_logs_seconds = 604800

# === GTID ===
gtid_mode = ON
enforce_gtid_consistency = ON

# === 只读配置 ===
read_only = ON
super_read_only = ON
relay_log = mysql-relay-bin
log_replica_updates = ON         # 从库更新也记录到自己的 binlog

# === 延迟复制（重要！）===
# 从库比主库延迟 1 小时，防止主库误删数据时从库也跟着删
replica_preserve_commit_order = ON

# === 复制过滤（如需要）===
# replicate_do_db = wo_property
```

### 2.4 主从复制初始化（首次搭建）

**步骤 1：主库开启 GTID 并记录当前位置**

```bash
# 在主库执行：
mysql -u root -p -e "
CHANGE REPLICATION SOURCE TO
  SOURCE_HOST='192.168.1.100',
  SOURCE_PORT=3306,
  SOURCE_USER='repl_user',
  SOURCE_PASSWORD='YourReplPassword123',
  SOURCE_AUTO_POSITION=1;
START REPLICA;
SHOW REPLICA STATUS\G
"
```

**步骤 2：创建复制账号（主库上执行）**

```sql
CREATE USER 'repl_user'@'%' IDENTIFIED BY 'YourReplPassword123';
GRANT REPLICATION SLAVE, REPLICATION CLIENT ON *.* TO 'repl_user'@'%';
FLUSH PRIVILEGES;
```

**步骤 3：验证复制状态**

```sql
SHOW REPLICA STATUS\G
```

关键检查项：
- `Replica_IO_Running`: 应为 `Yes`
- `Replica_SQL_Running`: 应为 `Yes`
- `Seconds_Behind_Replica`: 应为 `0` 或很小
- `Executed_Gtid_Set`: 应有值

### 2.5 延迟复制配置（核心！）

在从库执行：

```sql
STOP REPLICA;
CHANGE REPLICATION SOURCE TO SOURCE_DELAY = 3600;  -- 延迟 1 小时
START REPLICA;
```

**作用：** 主库误删数据时，从库保留了 1 小时前的完整数据，可用于恢复。

> **警告：** 延迟复制只对 SQL 线程有效，IO 线程仍是实时拉取binlog。实际延迟取决于 `Seconds_Behind_Replica` 值。

### 2.6 故障切换流程

**场景：主库（Master）挂了**

```
1. 确认主库不可用（多次 ping / mysql 连接超时）
2. 检查从库状态：
   SHOW REPLICA STATUS\G
   确认 Seconds_Behind_Replica 在可接受范围
3. 停止从库复制：
   STOP REPLICA;
4. 将从库提升为主库：
   SET GLOBAL read_only = OFF;
   SET GLOBAL super_read_only = OFF;
5. 更新应用数据库连接串，指向新主库 IP
6. 通知运维，记录故障时间
7. 旧主库修复后，作为新从库重新加入：
   CHANGE REPLICATION SOURCE TO SOURCE_HOST='新主库IP', SOURCE_PORT=3306, SOURCE_AUTO_POSITION=1;
   START REPLICA;
```

---

## 3. 备份策略设计

### 3.1 备份类型与周期

| 备份类型 | 频率 | 时间 | 保留份数 | 说明 |
|----------|------|------|----------|------|
| **全量备份** | 每周一次 | 周日凌晨 3:00 | 4 份（1个月） | xtrabackup 物理备份 |
| **增量备份** | 每天一次 | 每天凌晨 2:00 | 7 份（1周） | 备份自上次全量后变化的数据 |
| **Binlog 备份** | 每小时一次 | 每小时 :05 | 168 份（7天） | 实时复制 binlog 到备份存储 |

### 3.2 备份时间线

```
周日 03:00  ── 全量备份 ──→  [#########]  保留 4 周
周一 02:00  ── 增量备份 ──→  [####      ]  保留 7 天
周二 02:00  ── 增量备份 ──→  [####      ]  保留 7 天
周三 02:00  ── 增量备份 ──→  [####      ]  保留 7 天
周四 02:00  ── 增量备份 ──→  [####      ]  保留 7 天
周五 02:00  ── 增量备份 ──→  [####      ]  保留 7 天
周六 02:00  ── 增量备份 ──→  [####      ]  保留 7 天
每小时:05   ── Binlog 备份 ──→  实时复制
```

### 3.3 备份脚本

**脚本路径：** `scripts/mysql_backup/`

```
scripts/
└── mysql_backup/
    ├── backup_full.sh          # 全量备份
    ├── backup_incr.sh          # 增量备份
    ├── backup_binlog.sh        # Binlog 备份
    ├── purge_old_backups.sh    # 清理过期备份
    └── restore_test.sh         # 恢复测试脚本
```

#### 3.3.1 全量备份脚本

**文件：** `scripts/mysql_backup/backup_full.sh`

```bash
#!/bin/bash
#==============================================================================
# 全量备份脚本 - 每周日 03:00 执行
# 使用 mysqldump + gzip 压缩
#==============================================================================

set -euo pipefail

# === 配置 ===
BACKUP_DIR="/data/mysql_backup/full"
DATE=$(date +%Y%m%d_%H%M%S)
RETENTION_DAYS=28
MYSQL_HOST="127.0.0.1"
MYSQL_PORT="3306"
MYSQL_USER="backup_user"
MYSQL_PASSWORD="YourBackupPassword123"
DB_NAME="wo_property"

# 创建备份目录
mkdir -p "${BACKUP_DIR}"

# 备份文件命名
BACKUP_FILE="${BACKUP_DIR}/${DB_NAME}_full_${DATE}.sql.gz"

echo "[$(date)] 开始全量备份 -> ${BACKUP_FILE}"

# 执行备份（使用锁表备份，减少锁影响）
mysqldump \
  -h"${MYSQL_HOST}" \
  -P"${MYSQL_PORT}" \
  -u"${MYSQL_USER}" \
  -p"${MYSQL_PASSWORD}" \
  --single-transaction \
  --routines \
  --triggers \
  --events \
  --master-data=2 \
  --flush-logs \
  "${DB_NAME}" | gzip > "${BACKUP_FILE}"

# 计算校验和
sha256sum "${BACKUP_FILE}" > "${BACKUP_FILE}.sha256"

# 验证备份文件大小（太小的可能是失败）
FILE_SIZE=$(stat -f%z "${BACKUP_FILE}" 2>/dev/null || stat -c%s "${BACKUP_FILE}" 2>/dev/null)
if [ "${FILE_SIZE}" -lt 1048576 ]; then
  echo "[ERROR] 备份文件异常小 (${FILE_SIZE} bytes)，备份可能失败"
  exit 1
fi

# 清理过期备份
find "${BACKUP_DIR}" -name "*.sql.gz" -mtime +${RETENTION_DAYS} -delete
find "${BACKUP_DIR}" -name "*.sha256" -mtime +${RETENTION_DAYS} -delete

echo "[$(date)] 全量备份完成: ${BACKUP_FILE} (大小: ${FILE_SIZE} bytes)"
```

#### 3.3.2 增量备份脚本

**文件：** `scripts/mysql_backup/backup_incr.sh`

```bash
#!/bin/bash
#==============================================================================
# 增量备份脚本 - 每天 02:00 执行
# 备份自上次全量/增量以来变化的数据（依赖 binlog）
#==============================================================================

set -euo pipefail

BACKUP_DIR="/data/mysql_backup/incremental"
DATE=$(date +%Y%m%d_%H%M%S)
RETENTION_DAYS=7
DB_NAME="wo_property"

mkdir -p "${BACKUP_DIR}"

# 获取最近的 binlog 位置（增量起点）
LAST_BACKUP_INFO=$(find "${BACKUP_DIR}" -name "*.incr" -type f -printf '%T@ %p\n' 2>/dev/null | sort -rn | head -1 | cut -d' ' -f2-)
if [ -z "${LAST_BACKUP_INFO}" ]; then
  # 无历史增量，提示需要先做全量
  echo "[WARN] 未找到历史增量备份，请确认已执行全量备份"
fi

# 刷新日志并记录当前位置
mysql -u root -p'YourRootPassword' -e "FLUSH LOGS;"
BINLOG_FILE=$(mysql -u root -p'YourRootPassword' -N -e "SHOW MASTER STATUS;" | awk '{print $1}')
BINLOG_POSITION=$(mysql -u root -p'YourRootPassword' -N -e "SHOW MASTER STATUS;" | awk '{print $2}')

INCR_FILE="${BACKUP_DIR}/${DB_NAME}_incr_${DATE}.info"
echo "BINLOG=${BINLOG_FILE}" > "${INCR_FILE}"
echo "POSITION=${BINLOG_POSITION}" >> "${INCR_FILE}"
echo "TIMESTAMP=${DATE}" >> "${INCR_FILE}"

# 清理过期文件
find "${BACKUP_DIR}" -name "*.info" -mtime +${RETENTION_DAYS} -delete

echo "[$(date)] 增量备份记录完成: ${INCR_FILE} (binlog: ${BINLOG_FILE}, pos: ${BINLOG_POSITION})"
```

#### 3.3.3 Binlog 备份脚本

**文件：** `scripts/mysql_backup/backup_binlog.sh`

```bash
#!/bin/bash
#==============================================================================
# Binlog 备份脚本 - 每小时执行
# 实时同步 binlog 到备份目录，防止主库 binlog 被清理后丢失
#==============================================================================

set -euo pipefail

SOURCE_BINLOG_DIR="/var/lib/mysql"          # Linux 主库 binlog 目录
# SOURCE_BINLOG_DIR="/usr/local/var/mysql"   # macOS Homebrew 目录
DEST_BINLOG_DIR="/data/mysql_backup/binlog"
RETENTION_DAYS=7

mkdir -p "${DEST_BINLOG_DIR}"

# 复制最新的 binlog 文件（排除正在写入的）
rsync -av --include='mysql-bin.[0-9]*' --exclude='*' \
  "${SOURCE_BINLOG_DIR}/" "${DEST_BINLOG_DIR}/"

# 清理过期 binlog
find "${DEST_BINLOG_DIR}" -name "mysql-bin.*" -mtime +${RETENTION_DAYS} -delete

echo "[$(date)] Binlog 备份完成"
```

### 3.4 备份存储策略

| 存储层 | 位置 | 用途 | 保留周期 |
|--------|------|------|----------|
| **本地磁盘** | `/data/mysql_backup/` | 快速恢复 | 全量 4 周 / 增量 1 周 |
| **异地（NAS/对象存储）** | 挂载到 `/mnt/backup_nas/` | 灾难保护 | 至少 30 天 |

**异地备份同步脚本：**

```bash
# 每日将备份同步到 NAS（crontab: 04:00 执行）
rsync -avz --delete /data/mysql_backup/ /mnt/backup_nas/mysql_backup/
```

---

## 4. 灾难恢复预案

### 4.1 RPO / RTO 目标

| 目标 | 值 | 实现方式 |
|------|-----|----------|
| RPO | ≤ 1 小时 | 每小时 binlog 备份 + 每日增量备份 |
| RTO | ≤ 4 小时 | 标准化恢复流程 + 定期演练 |

### 4.2 恢复场景与步骤

#### 场景 A：主库数据文件损坏（未启用主从）

**预计时间：2-3 小时（取决于数据量）**

```
1. 停止 MySQL 服务
   sudo systemctl stop mysql   # Linux
   brew services stop mysql    # macOS

2. 清理损坏的数据目录（保留 ibdata1 外的配置）
   mv /var/lib/mysql /var/lib/mysql_broken_$(date +%Y%m%d)

3. 解压最新的全量备份
   gzip -d /data/mysql_backup/full/wo_property_full_20260504_030000.sql.gz

4. 恢复全量数据
   mysql -u root -p < /data/mysql_backup/full/wo_property_full_20260504_030000.sql

5. 应用增量 binlog（如果有）
   mysqlbinlog mysql-bin.000001 mysql-bin.000002 | mysql -u root -p

6. 启动 MySQL
   sudo systemctl start mysql

7. 验证数据完整性
   mysql -u root -p -e "SELECT COUNT(*) FROM wo_property.users;"

8. 通知相关方恢复完成
```

#### 场景 B：主库宕机，从库接管（主从正常）

**预计时间：15-30 分钟**

```
1. 确认主库不可用（至少 3 次 ping 超时）

2. 在从库检查数据状态
   SHOW REPLICA STATUS\G
   确认：
   - Replica_IO_Running = Yes
   - Replica_SQL_Running = Yes
   - Seconds_Behind_Replica < 3600（延迟在 1 小时内）

3. 停止从库复制（停止写入前的最后同步）
   STOP REPLICA;

4. 提升从库为主库
   SET GLOBAL read_only = OFF;
   SET GLOBAL super_read_only = OFF;

5. 修改应用数据库连接串（参考 docker-compose.yml 中的 DATABASE_URL）
   指向新的主库 IP

6. 重启应用服务验证连接

7. 记录故障时间、影响范围、恢复时间
```

#### 场景 C：误删除数据（利用延迟从库恢复）

**预计时间：1-2 小时**

```
前提：延迟从库配置为延迟 1 小时

1. 确认误删除发生时间 T

2. 连接到延迟从库

3. 停止复制（停在 T-30min 的位置，即误删除前 30 分钟）
   STOP REPLICA;
   SOURCE_DELAY = 0  # 等待追上
   # 或者直接：
   CHANGE REPLICATION SOURCE TO SOURCE_DELAY=0;
   START REPLICA;
   # 等待追上后：
   STOP REPLICA;

4. 导出误删除前的数据
   mysqldump wo_property users > users_before_T.sql

5. 在主库恢复数据
   mysql -u root -p wo_property < users_before_T.sql

6. 验证数据已恢复
```

### 4.3 恢复演练计划

| 周期 | 演练内容 | 时长 |
|------|----------|------|
| 每月 | 从备份文件恢复数据库（验证备份可用性） | 2 小时 |
| 每季度 | 模拟主库宕机，从库切换验证 | 4 小时 |
| 每年 | 完整灾难恢复演练（模拟 RTO 计时） | 半天 |

**演练检查清单：**

- [ ] 备份文件完整性（校验和验证）
- [ ] 恢复脚本可执行（无语法错误）
- [ ] 数据完整性（抽样验证）
- [ ] RTO 实际耗时记录
- [ ] 演练报告存档

---

## 5. 监控告警设计

### 5.1 Prometheus 监控指标

在 MySQL 主从服务器上部署 `mysqld_exporter`，抓取以下指标：

**关键告警规则（prometheus 规则）：**

```yaml
# prometheus/rules/mysql_rules.yml

groups:
  - name: mysql_backup_alerts
    rules:
      # 备份任务失败告警
      - alert: MySQLBackupFailed
        expr: mysql_backup_last_run_success == 0
        for: 5m
        labels:
          severity: critical
        annotations:
          summary: "MySQL 备份失败"
          description: "备份任务已 5 分钟未成功执行，请检查！"

      # 从库延迟告警
      - alert: MySQLReplicaLagHigh
        expr: mysql_replica_seconds_behind_master > 30
        for: 2m
        labels:
          severity: warning
        annotations:
          summary: "MySQL 从库复制延迟过高"
          description: "从库延迟 {{ $value }} 秒，超过 30 秒阈值"

      # 从库复制中断告警
      - alert: MySQLReplicaIOFailed
        expr: mysql_replica_io_running != 1
        for: 1m
        labels:
          severity: critical
        annotations:
          summary: "MySQL 从库复制中断"
          description: "从库 IO 线程中断，请立即检查！"

      # Binlog 磁盘空间告警
      - alert: MySQLBinlogDiskSpaceLow
        expr: (node_filesystem_size_bytes{mountpoint="/var/lib/mysql"} - node_filesystem_free_bytes_bytes{mountpoint="/var/lib/mysql"}) / node_filesystem_size_bytes{mountpoint="/var/lib/mysql"} > 0.8
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "MySQL binlog 磁盘空间不足"
          description: "磁盘使用率超过 80%，请及时清理或扩展"

      # 主库不可用告警
      - alert: MySQLMasterDown
        expr: up{job="mysql_master"} == 0
        for: 1m
        labels:
          severity: critical
        annotations:
          summary: "MySQL 主库不可用"
          description: "主库已失联，请检查服务状态！"
```

### 5.2 Grafana 面板

**面板 1：备份状态**

| 指标 | 图表类型 |
|------|----------|
| 备份任务成功率（最近 30 天） | Stat |
| 最后一次备份时间 | Time series |
| 备份文件大小趋势 | Time series |

**面板 2：主从复制状态**

| 指标 | 图表类型 |
|------|----------|
| 从库延迟（秒） | Time series |
| Binlog 位置差距 | Time series |
| 复制线程状态 | Table |

**面板 3：数据库健康**

| 指标 | 图表类型 |
|------|----------|
| 连接数 | Time series |
| QPS | Time series |
| 慢查询数量 | Time series |
| 磁盘使用率 | Gauge |

### 5.3 企业微信机器人告警

**Webhook 配置脚本：**

```bash
# scripts/mysql_backup/send_alert.sh

#!/bin/bash
#==============================================================================
# 企业微信告警脚本
#==============================================================================

WEBHOOK_URL="https://qyapi.weixin.qq.com/cgi-bin/webhook/send?key=YOUR_WEBHOOK_KEY"
LEVEL="${1:-info}"  # critical / warning / info
MESSAGE="${2:-Hello}"

case "${LEVEL}" in
  critical)
    COLOR="red"
    ;;
  warning)
    COLOR="yellow"
    ;;
  *)
    COLOR="green"
    ;;
esac

PAYLOAD=$(cat <<EOF
{
  "msgtype": "markdown",
  "markdown": {
    "content": "## 🛡️ MySQL 告警\n**级别：** ${LEVEL}\n**消息：** ${MESSAGE}\n**时间：** $(date '+%Y-%m-%d %H:%M:%S')"
  }
}
EOF
)

curl -s -X POST "${WEBHOOK_URL}" \
  -H "Content-Type: application/json" \
  -d "${PAYLOAD}"
```

**Prometheus 告警接收器配置（Alertmanager）：**

```yaml
# prometheus/alertmanager.yml

route:
  group_by: ['alertname']
  receiver: 'wechat'

receivers:
  - name: 'wechat'
    webhook_configs:
      - url: 'http://alert-forwarder:5000/alert'

# 告警路由示例
inhibit_rules:
  - source_match:
      severity: 'critical'
    target_match:
      severity: 'warning'
    equal: ['alertname']
```

---

## 6. 脚本清单

| 脚本路径 | 用途 | 执行频率 |
|----------|------|----------|
| `scripts/mysql_backup/backup_full.sh` | 全量备份 | 每周日 03:00 |
| `scripts/mysql_backup/backup_incr.sh` | 增量备份 | 每天 02:00 |
| `scripts/mysql_backup/backup_binlog.sh` | Binlog 备份 | 每小时 :05 |
| `scripts/mysql_backup/purge_old_backups.sh` | 清理过期备份 | 每周一 04:00 |
| `scripts/mysql_backup/restore_test.sh` | 恢复测试 | 每月一次 |
| `scripts/mysql_backup/send_alert.sh` | 企业微信告警 | 由监控触发 |

### 6.1 Crontab 配置（Linux 生产服务器）

```cron
# MySQL 备份任务
0 2 * * * /Users/mac/Projects/WO-Property-Management/scripts/mysql_backup/backup_incr.sh >> /var/log/mysql_backup/incr.log 2>&1
0 3 * * 0 /Users/mac/Projects/WO-Property-Management/scripts/mysql_backup/backup_full.sh >> /var/log/mysql_backup/full.log 2>&1
5 * * * * /Users/mac/Projects/WO-Property-Management/scripts/mysql_backup/backup_binlog.sh >> /var/log/mysql_backup/binlog.log 2>&1
0 4 * * 1 /Users/mac/Projects/WO-Property-Management/scripts/mysql_backup/purge_old_backups.sh >> /var/log/mysql_backup/purge.log 2>&1
```

### 6.2 创建备份用户（主从通用）

```sql
-- 在主库执行
CREATE USER 'backup_user'@'localhost' IDENTIFIED BY 'YourBackupPassword123';
GRANT SELECT, LOCK TABLES, REPLICATION CLIENT ON *.* TO 'backup_user'@'localhost';
FLUSH PRIVILEGES;
```

---

## 7. 实施路线图

| 阶段 | 时间 | 任务 | 交付物 |
|------|------|------|--------|
| **Phase 1** | 第 1 周 | 搭建主从复制，初始化同步 | 主从复制正常，延迟配置完成 |
| **Phase 2** | 第 2 周 | 部署全量/增量备份脚本 | 备份脚本可执行，cron 配置完成 |
| **Phase 3** | 第 3 周 | 部署 Prometheus + Grafana 监控 | 监控面板可用，告警可触发 |
| **Phase 4** | 第 4 周 | 完成第一次恢复演练 | 演练报告，RTO 实测数据 |
| **Ongoing** | 每月 | 备份验证 + 监控优化 | 月度报告 |

---

## 附录

### A. 参考命令

```sql
-- 查看主库状态
SHOW MASTER STATUS;

-- 查看从库复制状态
SHOW REPLICA STATUS\G

-- 查看 binlog 列表
SHOW BINARY LOGS;

-- 查看当前连接数
SHOW STATUS LIKE 'Threads_connected';

-- 查看慢查询
SHOW VARIABLES LIKE 'slow_query%';
```

### B. 关键文件路径参考

| 文件 | 路径 |
|------|------|
| MySQL 配置文件（macOS） | `/usr/local/etc/my.cnf` |
| MySQL 配置文件（Linux） | `/etc/mysql/my.cnf` |
| Binlog 目录（macOS） | `/usr/local/var/mysql/` |
| Binlog 目录（Linux） | `/var/lib/mysql/` |
| 备份存储目录 | `/data/mysql_backup/` |
| 监控数据 | `/var/lib/prometheus/`

---

_文档版本：v1.0 | 待运维团队评审后实施_