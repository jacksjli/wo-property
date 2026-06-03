#!/bin/bash
#===============================================================================
# WO-Property 备份脚本 v3.0
# 用途：备份源码、设计文档、脚本（排除 node_modules/bin/obj）
# 触发：每日 20:00 自动（cron）或手动运行
#
# 备份恢复后需要：
#   npm install（前端依赖）
#   dotnet build（后端编译，自动）
#===============================================================================

set -e

PROJECT_DIR="/Users/mac/Projects/WO-Property-Management"
BACKUP_DIR="$HOME/Desktop/WO-Property-备份-$(date '+%Y-%m-%d')"
TIMESTAMP=$(date '+%Y-%m-%d %H:%M:%S')

echo "=========================================="
echo "WO-Property 备份开始 — $TIMESTAMP"
echo "=========================================="

# 创建备份目录
mkdir -p "$BACKUP_DIR"

# 统一排除列表（适用于所有 rsync）
EXCLUDE_ARGS=(
  --exclude='bin/'
  --exclude='obj/'
  --exclude='.git/'
  --exclude='node_modules/'
  --exclude='dist/'
  --exclude='.vs/'
  --exclude='*.user'
  --exclude='*.log'
  --exclude='logs/'
  --exclude='.idea/'
  --exclude='*.suo'
  --exclude='*.useross'
  --exclude='packages/'
)

# 1. 后端全部服务源码
echo "[1/5] 备份后端服务源码..."
mkdir -p "$BACKUP_DIR/src"
rsync -av "${EXCLUDE_ARGS[@]}" \
  "$PROJECT_DIR/src/" "$BACKUP_DIR/src/" 2>/dev/null || true

# 2. admin-portal 前端
echo "[2/5] 备份 admin-portal..."
mkdir -p "$BACKUP_DIR/admin-portal"
rsync -av "${EXCLUDE_ARGS[@]}" \
  "$PROJECT_DIR/src/admin-portal/" "$BACKUP_DIR/admin-portal/" 2>/dev/null || true

# 3. 小程序源码
echo "[3/5] 备份小程序源码..."
mkdir -p "$BACKUP_DIR/woa-property-mini"
rsync -av "${EXCLUDE_ARGS[@]}" \
  "$PROJECT_DIR/src/woa-property-mini/src/" "$BACKUP_DIR/woa-property-mini/src/" 2>/dev/null || true

# 4. 设计文档
echo "[4/5] 备份设计文档..."
mkdir -p "$BACKUP_DIR/docs"
rsync -av "${EXCLUDE_ARGS[@]}" \
  "$PROJECT_DIR/docs/" "$BACKUP_DIR/docs/" 2>/dev/null || true

# 5. 脚本和工具
echo "[5/5] 备份脚本和工具..."
mkdir -p "$BACKUP_DIR/scripts"
rsync -av "${EXCLUDE_ARGS[@]}" \
  "$PROJECT_DIR/scripts/" "$BACKUP_DIR/scripts/" 2>/dev/null || true

mkdir -p "$BACKUP_DIR/init-scripts"
rsync -av "${EXCLUDE_ARGS[@]}" \
  "$PROJECT_DIR/init-scripts/" "$BACKUP_DIR/init-scripts/" 2>/dev/null || true

# 复制工作日志
if [ -f "$HOME/.openclaw/workspace/memory/$(date '+%Y-%m-%d').md" ]; then
  cp "$HOME/.openclaw/workspace/memory/$(date '+%Y-%m-%d').md" "$BACKUP_DIR/daily-log.md"
fi

# 计算备份大小
BACKUP_SIZE=$(du -sh "$BACKUP_DIR" 2>/dev/null | cut -f1)

# 生成备份说明
cat > "$BACKUP_DIR/备份说明.txt" << EOF
===========================================
WO-Property 备份清单 (v3.0)
备份时间：$TIMESTAMP
备份大小：$BACKUP_SIZE
===========================================

【包含内容】
├── src/                     后端全部服务源码（.cs 源码）
├── admin-portal/            管理后台源码（排除 node_modules）
├── woa-property-mini/src/   微信小程序源码（排除 node_modules/dist）
├── docs/                    设计文档、API标准、部署指南等
├── scripts/                 构建、部署、诊断脚本
├── init-scripts/            数据库初始化脚本
└── daily-log.md             今日工作日志

【不包括】
- node_modules/   （npm install 恢复）
- dist/           （npm run build 恢复）
- bin/ obj/       （dotnet build 自动编译）
- .git/           （版本控制目录）
- logs/ *.log     （日志文件）

【恢复步骤】
1. 把备份目录复制到新 Mac 的任意位置
2. 进入项目目录：
   cd WO-Property-Management
3. 安装前端依赖：
   cd src/admin-portal && npm install && cd ../..
   cd src/woa-property-mini && npm install && cd ../..
4. 启动所有服务（使用 PM2）：
   pm2 start ecosystem.config.js
   pm2 save
5. 配置开机自启动（一次性）：
   sudo env PATH=\$PATH:/usr/local/bin /Users/mac/.npm-global/lib/node_modules/pm2/bin/pm2 startup launchd -u mac --hp /Users/mac

【备份脚本位置】
scripts/backup.sh （本脚本）

===========================================
EOF

echo ""
echo "=========================================="
echo "✅ 备份完成"
echo "📁 位置：$BACKUP_DIR"
echo "📦 大小：$BACKUP_SIZE"
echo "=========================================="

# 输出备份内容大小
echo ""
echo "【备份内容明细】"
du -sh "$BACKUP_DIR"/*/ 2>/dev/null | sort -rh | head -10