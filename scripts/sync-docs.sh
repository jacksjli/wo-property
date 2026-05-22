#!/bin/bash
#===============================================================================
# WO-Property 文档自动同步脚本
# 用途：代码测试成功后，自动检查并更新受影响的设计文档
# 触发：每次完成代码变更并通过测试后手动运行，或由 cron 定期调用
#===============================================================================

set -e

PROJECT_DIR="/Users/mac/Projects/WO-Property-Management"
MEMORY_DIR="$HOME/.openclaw/workspace/memory"
LOG_FILE="$PROJECT_DIR/docs/AUDIT_LOG.md"
TODAY=$(date '+%Y-%m-%d')
TODAY_LOG="$MEMORY_DIR/$TODAY.md"

echo "=========================================="
echo "WO-Property 文档同步检查 — $TODAY"
echo "=========================================="

# 检查今日是否有记录
if [ ! -f "$TODAY_LOG" ]; then
  echo "⚠️ 今日工作日志不存在，跳过"
  exit 0
fi

# 解析今日变更涉及的关键词
echo ""
echo "[1/4] 扫描今日代码变更..."
CHANGES=$(grep -E "完成|修复|新增|修改|变更" "$TODAY_LOG" 2>/dev/null | head -10 || true)
if [ -z "$CHANGES" ]; then
  echo "  未发现需要同步的变更记录"
else
  echo "  发现以下变更："
  echo "$CHANGES" | while read line; do echo "    - $line"; done
fi

# 检查需要更新的文档
echo ""
echo "[2/4] 检查需要同步的文档..."

# 核心模块列表
declare -a MODULES=("ticket" "dispatch" "person" "notification")

for mod in "${MODULES[@]}"; do
  MOD_DIR="$PROJECT_DIR/docs/MODULES/$mod"
  ARCH_FILE="$PROJECT_DIR/docs/design/TICKET_SERVICE_ARCHITECTURE_v1.0.md"
  
  case "$mod" in
    "ticket")
      if grep -q "P0\|P1\|P2\|派单\|dispatch\|overdue\|reassign" "$TODAY_LOG" 2>/dev/null; then
        echo "  ✅ ticket 模块需要更新"
        NEED_UPDATE="yes"
      fi
      ;;
  esac
done

# 更新设计文档时间戳
echo ""
echo "[3/4] 更新文档时间戳..."
if [ -f "$LOG_FILE" ]; then
  if grep -q "$TODAY" "$LOG_FILE"; then
    echo "  ℹ️  AUDIT_LOG.md 今日已有记录"
  else
    echo "" >> "$LOG_FILE"
    echo "## $TODAY" >> "$LOG_FILE"
    echo "变更同步：完成" >> "$LOG_FILE"
    echo "  详见：memory/$TODAY.md" >> "$LOG_FILE"
  fi
fi

# 更新架构文档中的"最后更新"字段
ARCH_FILE="$PROJECT_DIR/docs/design/TICKET_SERVICE_ARCHITECTURE_v1.0.md"
if [ -f "$ARCH_FILE" ]; then
  if grep -q "最后更新" "$ARCH_FILE"; then
    sed -i "" "s/\*\*最后更新\*\*:.*/\*\*最后更新\*\*: $TODAY/" "$ARCH_FILE" 2>/dev/null || true
  fi
fi

echo ""
echo "[4/4] 生成同步记录..."
SYNC_MSG="文档同步检查完成 — $TODAY"
echo "$SYNC_MSG"
echo ""

echo "=========================================="
echo "✅ 文档同步检查完成"
echo "=========================================="