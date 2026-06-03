#!/bin/bash
# module-consistency-check.sh
# 新增模块架构一致性自动检查
# 触发：心跳 cron 或人工执行

set -e

PROJECT_ROOT="/Users/mac/Projects/WO-Property-Management"
cd "$PROJECT_ROOT"

TODAY=$(date +%Y-%m-%d)
REPORT_FILE="$PROJECT_ROOT/docs/audit/MODULE_CONSISTENCY_$TODAY.md"
ISSUES=()

echo "🔍 WO 物业管理软件 — 模块架构一致性检查"
echo "=========================================="
echo "时间: $(date '+%Y-%m-%d %H:%M:%S')"
echo ""

# ============ P0: 硬编码 IP 检查 ============
echo "【P0】硬编码 IP 检查..."
HARDCODED=$(grep -rn "http://192\.168\." \
  "$PROJECT_ROOT/src/admin-portal/src/api/" \
  "$PROJECT_ROOT/src/admin-portal/src/views/" \
  "$PROJECT_ROOT/src/woa-property-mini/src/" \
  --include="*.ts" --include="*.vue" --include="*.js" \
  2>/dev/null | grep -v "node_modules" | grep -v "env.js" || true)

if [ -n "$HARDCODED" ]; then
  echo "🔴 P0: 发现硬编码 IP（必须立即修复）"
  echo "$HARDCODED" | while read line; do
    echo "   $line"
    ISSUES+=("🔴 P0 硬编码 IP: $line")
  done
else
  echo "✅ P0: 无硬编码 IP"
fi

# ============ P1: API 文件 getServiceUrl 检查 ============
echo ""
echo "【P1】API 文件 getServiceUrl 调用检查..."
API_DIR="$PROJECT_ROOT/src/admin-portal/src/api"
for f in "$API_DIR"/*.ts; do
  [ -f "$f" ] || continue
  filename=$(basename "$f")
  # 跳过 config.ts 和 http.ts 本身
  [ "$filename" = "config.ts" ] && continue
  [ "$filename" = "http.ts" ] && continue
  
  # 检查是否调用了 getServiceUrl（某些文件如 auth.ts 可能有独立 URL 定义，需要具体分析）
  # 通用规则：如果文件中使用了 createHttpClient 且包含 URL 字符串，则检查是否从 config 导入
  has_createHttpClient=$(grep -c "createHttpClient" "$f" || true)
  if [ "$has_createHttpClient" -gt 0 ]; then
    has_getServiceUrl=$(grep -c "getServiceUrl" "$f" || true)
    if [ "$has_getServiceUrl" -eq 0 ]; then
      echo "🔴 P1: $filename 使用 createHttpClient 但未调用 getServiceUrl"
      ISSUES+=("🔴 P1 未使用 getServiceUrl: $filename")
    fi
  fi
done

# ============ P1: health 接口检查 ============
echo ""
echo "【P1】健康检查接口检查..."
HEALTH_CHECK=$(grep -rn "health" \
  "$PROJECT_ROOT/src"/*/Program.cs \
  2>/dev/null | grep -v "node_modules" | grep -c "MapGet.*health" || true)

if [ "$HEALTH_CHECK" -eq 0 ]; then
  echo "🟡 P1: 部分服务可能缺少健康检查"
  ISSUES+=("🟡 P1: 部分服务缺少健康检查接口")
else
  echo "✅ P1: 健康检查接口已配置"
fi

# ============ P1: canonical-schema.sql 表定义检查 ============
echo ""
echo "【P1】canonical-schema.sql 表定义检查..."
# 检查最近修改的 Controller 对应的表是否在 schema 中
CONTROLLERS=$(find "$PROJECT_ROOT/src" -name "*Controller.cs" -newer "$PROJECT_ROOT/docs/MODULES/ARCHITECTURE_STANDARDS.md" 2>/dev/null)
if [ -n "$CONTROLLERS" ]; then
  echo "🟡 P1: 发现新增 Controller，需确认表已写入 canonical-schema.sql"
  ISSUES+=("🟡 P1: 新增 Controller 需确认表定义已写入 canonical-schema.sql")
else
  echo "✅ P1: 无新增 Controller"
fi

# ============ P1: Authorize 检查 ============
echo ""
echo "【P1】Authorize 装饰器检查..."
MISSING_AUTH=$(grep -rn "class.*Controller" \
  "$PROJECT_ROOT/src/WO.Property.*Service/Controllers/" \
  2>/dev/null | while read line; do
    ctrlr=$(echo "$line" | cut -d: -f1)
    has_auth=$(grep -c "\[Authorize\]" "$ctrlr" || true)
    if [ "$has_auth" -eq 0 ]; then
      echo "$ctrlr"
    fi
  done)

if [ -n "$MISSING_AUTH" ]; then
  echo "🟡 P1: 以下 Controller 缺少 [Authorize]："
  echo "$MISSING_AUTH" | while read f; do echo "   $f"; done
  ISSUES+=("🟡 P1: 缺少 Authorize 装饰器")
else
  echo "✅ P1: 所有 Controller 都有 [Authorize]"
fi

# ============ P2: 日志检查 ============
echo ""
echo "【P2】日志规范检查..."
LOG_CHECK=$(grep -rn "LogInformation" \
  "$PROJECT_ROOT/src/WO.Property.*Service/Controllers/" \
  2>/dev/null | wc -l | tr -d ' ')
if [ "$LOG_CHECK" -gt 0 ]; then
  echo "✅ P2: 发现 $LOG_CHECK 处日志记录"
else
  echo "🟢 P2: 无日志（可选优化项）"
fi

# ============ P2: 表名一致性 ============
echo ""
echo "【P2】表名与 canonical-schema.sql 一致性检查..."
# 获取代码中所有 "FROM" 或 "INTO" 的表名，与 schema 对比
# 简化版：检查 SQL 中是否有混用大小写的情况
TABLE_NAMING=$(grep -rn "FROM\|INTO\|UPDATE\|DELETE FROM" \
  "$PROJECT_ROOT/src/WO.Property.*Service/Controllers/" \
  --include="*.cs" 2>/dev/null | \
  grep -E "(FROM|INTO|UPDATE|DELETE FROM)\s+[A-Z]" | \
  grep -v "FROM\s+Enum\|FROM\s+JobType\|FROM\s+Regions\|FROM\s+Areas" || true)

if [ -n "$TABLE_NAMING" ]; then
  echo "🟡 P2: 发现 PascalCase 表名调用（需确认与 schema 一致）："
  echo "$TABLE_NAMING" | head -5 | while read line; do echo "   $line"; done
  ISSUES+=("🟡 P2: 表名大小写需确认与 canonical-schema.sql 一致")
else
  echo "✅ P2: 表名使用正常"
fi

# ============ 生成报告 ============
echo ""
echo "=========================================="
echo "📊 检查报告"
echo "=========================================="

P0_COUNT=$(echo "${ISSUES[@]}" | grep -c "🔴 P0" || true)
P1_COUNT=$(echo "${ISSUES[@]}" | grep -c "🔴 P1\|🟡 P1" || true)
P2_COUNT=$(echo "${ISSUES[@]}" | grep -c "🟡 P2\|🟢 P2" || true)

echo "🔴 P0 问题: $P0_COUNT"
echo "🟡 P1 问题: $P1_COUNT"
echo "🟢 P2/观察: $P2_COUNT"
echo ""

if [ ${#ISSUES[@]} -gt 0 ]; then
  echo "问题列表："
  for issue in "${ISSUES[@]}"; do
    echo "  $issue"
  done
  echo ""
  echo "📝 报告已保存: $REPORT_FILE"
else
  echo "✅ 所有检查通过，无问题"
fi

# ============ 写入报告文件 ============
mkdir -p "$(dirname $REPORT_FILE)"
{
  echo "# 模块架构一致性检查报告"
  echo ""
  echo "**检查时间：** $(date '+%Y-%m-%d %H:%M:%S')"
  echo ""
  echo "## 统计"
  echo ""
  echo "| 级别 | 数量 |"
  echo "|------|------|"
  echo "| 🔴 P0 | $P0_COUNT |"
  echo "| 🟡 P1 | $P1_COUNT |"
  echo "| 🟢 P2 | $P2_COUNT |"
  echo ""
  echo "## 问题列表"
  echo ""
  if [ ${#ISSUES[@]} -gt 0 ]; then
    for issue in "${ISSUES[@]}"; do
      echo "- $issue"
    done
  else
    echo "✅ 所有检查通过，无问题"
  fi
  echo ""
  echo "---"
  echo "*由 HEARTBEAT 自动触发*"
} > "$REPORT_FILE"


# ============ P0: MySQL 表名大小写检查（仅 MasterDataService） ============
echo ""
echo "【P0】MySQL 表名大小写检查（MasterDataService）..."
MD_CONTROLLERS="$PROJECT_ROOT/src/WO.Property.MasterDataService/Controllers"
if [ -d "$MD_CONTROLLERS" ]; then
  CASESENSITIVE_TABLES="ModuleFields|FieldDefinitions|EnumDefinitions"
  
  VIOLATIONS=$(grep -rnE "(FROM|INTO|UPDATE|DELETE FROM)\\s+($CASESENSITIVE_TABLES)\\s"     "$MD_CONTROLLERS"/ --include="*.cs" 2>/dev/null | grep -v "//" || true)
  
  if [ -n "$VIOLATIONS" ]; then
    echo "🔴 P0: MasterDataService 使用了大小写敏感的 PascalCase 表名（必须用小写）："
    echo "$VIOLATIONS" | while read line; do
      echo "   $line"
      ISSUES+=("🔴 P0 MySQL表名大小写: $line")
    done
  else
    echo "✅ P0: MasterDataService 表名使用小写"
  fi
fi

exit 0

# ============ P0: MySQL 表名大小写检查（仅 MasterDataService） ============
echo ""
echo "【P0】MySQL 表名大小写检查（MasterDataService）..."
MD_CONTROLLERS="$PROJECT_ROOT/src/WO.Property.MasterDataService/Controllers"
if [ -d "$MD_CONTROLLERS" ]; then
  # 检查是否使用了已知的 PascalCase 大小写敏感表名
  # 这些表在 macOS MySQL + MySqlConnector 下必须用小写
  CASESENSITIVE_TABLES="ModuleFields|FieldDefinitions|EnumDefinitions"
  
  VIOLATIONS=$(grep -rnE "(FROM|INTO|UPDATE|DELETE FROM)\s+($CASESENSITIVE_TABLES)\s" \
    "$MD_CONTROLLERS"/ --include="*.cs" 2>/dev/null | grep -v "//" || true)
  
  if [ -n "$VIOLATIONS" ]; then
    echo "🔴 P0: MasterDataService 使用了大小写敏感的 PascalCase 表名（必须用小写）："
    echo "$VIOLATIONS" | while read line; do
      echo "   $line"
      ISSUES+=("🔴 P0 MySQL表名大小写: $line")
    done
  else
    echo "✅ P0: MasterDataService 表名使用小写"
  fi
fi
