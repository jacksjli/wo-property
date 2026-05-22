#!/bin/bash
#===============================================================================
# WO-Property 全量备份脚本 v2.0
# 用途：备份全部源码、设计文档、变更记录到桌面
# 触发：每日 20:00 自动（cron）或手动运行
#===============================================================================

set -e

PROJECT_DIR="/Users/mac/Projects/WO-Property-Management"
BACKUP_DIR="$HOME/Desktop/WO-Property-备份-$(date '+%Y-%m-%d')"
TIMESTAMP=$(date '+%Y-%m-%d %H:%M:%S')

echo "=========================================="
echo "WO-Property 全量备份开始 — $TIMESTAMP"
echo "=========================================="

# 创建备份目录
mkdir -p "$BACKUP_DIR"

# 1. 全部源码（所有 40+ 服务，排除 bin/obj/node_modules/.git）
echo "[1/5] 备份全部源码..."
rsync -av \
  --exclude='bin/' \
  --exclude='obj/' \
  --exclude='.git/' \
  --exclude='node_modules/' \
  --exclude='logs/' \
  --exclude='*.log' \
  --exclude='.vs/' \
  --exclude='*.user' \
  "$PROJECT_DIR/src/" "$BACKUP_DIR/src-all/" 2>/dev/null || true

# 2. 前端完整源码
echo "[2/5] 备份前端源码..."
rsync -av \
  --exclude='node_modules/' \
  --exclude='dist/' \
  --exclude='.git/' \
  "$PROJECT_DIR/src/admin-portal/" "$BACKUP_DIR/admin-portal/" 2>/dev/null || true

# 3. 设计文档（全部）
echo "[3/5] 备份设计文档..."
mkdir -p "$BACKUP_DIR/docs"
rsync -av "$PROJECT_DIR/docs/" "$BACKUP_DIR/docs/" 2>/dev/null || true

# 4. 脚本和工具
echo "[4/5] 备份脚本和工具..."
mkdir -p "$BACKUP_DIR/scripts"
rsync -av "$PROJECT_DIR/scripts/" "$BACKUP_DIR/scripts/" 2>/dev/null || true
rsync -av "$PROJECT_DIR/init-scripts/" "$BACKUP_DIR/init-scripts/" 2>/dev/null || true

# 5. 变更日志 + 工作日志
echo "[5/5] 备份变更记录..."
cp "$PROJECT_DIR/docs/AUDIT_LOG.md" "$BACKUP_DIR/AUDIT_LOG.md" 2>/dev/null || true

# 今日工作日志
if [ -f "$HOME/.openclaw/workspace/memory/$(date '+%Y-%m-%d').md" ]; then
  cp "$HOME/.openclaw/workspace/memory/$(date '+%Y-%m-%d').md" "$BACKUP_DIR/daily-log.md"
fi

# 生成备份清单
cat > "$BACKUP_DIR/备份说明.txt" << EOF
WO-Property 全量备份清单
备份时间：$TIMESTAMP
备份目录：$BACKUP_DIR

包含内容：
├── src-all/               全部后端服务源码（40+ 服务，排除 bin/obj）
├── admin-portal/          前端 admin-portal 完整源码（排除 node_modules）
├── docs/                  全部设计文档（design/ + MODULES/ + audit/）
├── scripts/               构建和部署脚本
├── init-scripts/          数据库初始化脚本
├── AUDIT_LOG.md           审计日志
└── daily-log.md           今日工作日志

不包括（过大）：
- bin/、obj/、node_modules/、dist/、.git/

恢复方法：
1. 找到需要的文件
2. 覆盖到 $PROJECT_DIR 对应路径
3. dotnet build 验证

自动化工具：
- scripts/backup.sh      本脚本
- scripts/post-test-hook.sh  测试成功后文档同步钩子
- scripts/sync-docs.sh   文档同步检查
EOF

echo ""
echo "=========================================="
echo "✅ 全量备份完成"
echo "📁 位置：$BACKUP_DIR"
echo "=========================================="