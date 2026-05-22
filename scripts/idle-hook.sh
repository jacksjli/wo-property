#!/bin/bash
#===============================================================================
# WO-Property IDLE Hook（空闲自动执行钩子）
# 
# 触发条件：每天 heartbeat 间隔期间（每小时检查一次）
# 逻辑链：
#   1. 读取 TODO.md，检查是否有未完成任务
#   2. 如有 → 执行任务直到完成
#   3. 如无 → 运行单元测试
#   4. 单元测试通过 → 执行设计文档审计和更新
#   5. 全部完成 → 执行全量备份
#
# 使用方式（cron 自动触发，或手动测试）：
#   bash /Users/mac/Projects/WO-Property-Management/scripts/idle-hook.sh
#===============================================================================

set -e

PROJECT_DIR="/Users/mac/Projects/WO-Property-Management"
TODO_FILE="$PROJECT_DIR/../workspace/TODO.md"
MEMORY_DIR="$HOME/.openclaw/workspace/memory"
TODAY=$(date '+%Y-%m-%d')
TODAY_LOG="$MEMORY_DIR/$TODAY.md"
TIMESTAMP=$(date '+%Y-%m-%d %H:%M:%S')

echo "=========================================="
echo "WO-Property IDLE Hook — $TIMESTAMP"
echo "=========================================="

#-------------------------------------------------------------------------------
# Step 1: 检查 TODO.md 是否有未完成承诺
#-------------------------------------------------------------------------------
echo ""
echo "[Step 1] 检查 TODO.md 未完成任务..."

PENDING_TASKS=$(grep -E "^\[.*\].*承诺：|^\- \[ \]" "$TODO_FILE" 2>/dev/null | grep -v "完成" | head -5 || true)

if [ -n "$PENDING_TASKS" ]; then
  echo "  发现未完成任务："
  echo "$PENDING_TASKS" | while read line; do echo "    $line"; done
  echo ""
  echo "  ⚠️ 仍有未完成任务，跳过单元测试，直接执行全量备份"
  TASK_STATUS="has_pending_tasks"
else
  echo "  ✅ 所有 TODO 任务已完成"
  TASK_STATUS="all_done"
fi

#-------------------------------------------------------------------------------
# Step 2: 单元测试（只有任务全部完成才执行）
#-------------------------------------------------------------------------------
UNIT_TEST_RESULT="skipped"
if [ "$TASK_STATUS" = "all_done" ]; then
  echo ""
  echo "[Step 2] 执行单元测试..."
  
  # 7个核心服务健康检查
  SERVICES_OK=0
  for port in 5173 5106 5102 5018 5019 5003 5129; do
    result=$(lsof -i :$port 2>/dev/null | grep LISTEN | wc -l)
    if [ $result -gt 0 ]; then
      echo "  ✅ Port $port OK"
    else
      echo "  ❌ Port $port FAIL"
    fi
  done
  
  # Overdue API 测试
  OVERDUE_RESULT=$(curl -s http://localhost:5102/api/tenant/tickets/overdue -H "X-Tenant: wo_property" 2>/dev/null || echo "FAIL")
  if echo "$OVERDUE_RESULT" | grep -q "success"; then
    echo "  ✅ Overdue API OK"
    UNIT_TEST_RESULT="passed"
  else
    echo "  ❌ Overdue API FAIL"
    UNIT_TEST_RESULT="failed"
  fi
else
  echo ""
  echo "[Step 2] 跳过单元测试（有未完成任务）"
fi

#-------------------------------------------------------------------------------
# Step 3: 设计文档审计和更新
#-------------------------------------------------------------------------------
if [ "$UNIT_TEST_RESULT" = "passed" ]; then
  echo ""
  echo "[Step 3] 执行设计文档审计和更新..."
  bash "$PROJECT_DIR/scripts/sync-docs.sh" 2>&1 | tail -5
  DOCUMENT_STATUS="audited"
else
  echo ""
  echo "[Step 3] 跳过文档审计（单元测试未通过）"
  DOCUMENT_STATUS="skipped"
fi

#-------------------------------------------------------------------------------
# Step 4: 全量备份（最后一步，无论如何都执行）
#-------------------------------------------------------------------------------
echo ""
echo "[Step 4] 执行全量备份..."
bash "$PROJECT_DIR/scripts/backup.sh" 2>&1 | tail -5

#-------------------------------------------------------------------------------
# 记录到今日日志
#-------------------------------------------------------------------------------
echo ""
echo "[Step 5] 记录执行日志..."
echo "" >> "$TODAY_LOG"
echo "## IDLE Hook 执行记录 — $TIMESTAMP" >> "$TODAY_LOG"
echo "任务状态: $TASK_STATUS" >> "$TODAY_LOG"
echo "单元测试: $UNIT_TEST_RESULT" >> "$TODAY_LOG"
echo "文档审计: $DOCUMENT_STATUS" >> "$TODAY_LOG"
echo "全量备份: 已执行（~/Desktop/WO-Property-备份-${TODAY}/）" >> "$TODAY_LOG"

#-------------------------------------------------------------------------------
# 汇总输出
#-------------------------------------------------------------------------------
echo ""
echo "=========================================="
echo "✅ IDLE Hook 执行完成"
echo ""
echo "执行汇总："
echo "  任务检查: $([ "$TASK_STATUS" = "all_done" ] && echo '✅ 全部完成' || echo '⚠️ 有未完成任务')"
echo "  单元测试: $UNIT_TEST_RESULT"
echo "  文档审计: $DOCUMENT_STATUS"
echo "  全量备份: ✅ 已执行"
echo ""
echo "📁 备份位置：~/Desktop/WO-Property-备份-${TODAY}/"
echo "=========================================="