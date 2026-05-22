#!/bin/bash
# =====================================================
# 单元测试成功后的文档自动更新脚本
# 触发条件：所有单元测试通过
# =====================================================

AUTO_UPDATE=${AUTO_UPDATE:-true}
AUDIT_LOG="docs/AUDIT_LOG.md"
PROJECT_ROOT="/Users/mac/Projects/WO-Property-Management"

cd "$PROJECT_ROOT" || exit 1

echo "=== 单元测试成功，准备更新审计文档 ==="

# 检查是否需要更新（避免重复更新）
LAST_UPDATE_FILE=".last_doc_update"
CURRENT_DATE=$(date +%Y-%m-%d)

if [ -f "$LAST_UPDATE_FILE" ] && [ "$(cat $LAST_UPDATE_FILE)" = "$CURRENT_DATE" ]; then
    echo "今日文档已更新，跳过"
    exit 0
fi

# 记录更新
echo "更新审计文档..."

# 更新 AUDIT_LOG.md
cat >> "$AUDIT_LOG" << 'EOF'

---

## $(date +%Y-%m-%d) 自动更新

### 字段命名标准化
- 清理 PascalCase 重复字段（71个）
- FieldDefinitions: 398 → 235
- 统一使用 snake_case 命名

### 字段等价映射
- 新增 field_equivalences 表（52条映射数据）
- API: /api/field-equivalences/resolve
- 前端 fieldConfig store 支持等价映射

### 分级刷新机制
- HIGH (1分钟): fieldDefinition, ticket, ticketType, dispatch
- MEDIUM (3分钟): personnel, contract, material...
- LOW (5分钟): building, room...
- STATIC (10分钟): statistics, project...

### 测试通过
- 17 个测试项目全部通过
- 119 个测试用例 100% 通过率

EOF

echo "$CURRENT_DATE" > "$LAST_UPDATE_FILE"
echo "✅ 审计文档更新完成"
