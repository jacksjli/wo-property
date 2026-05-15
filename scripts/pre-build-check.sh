#!/bin/bash
# =============================================================================
# WO-Property 编译前规范检查
#
# 检查逻辑（只检查真正的违规，不检查已知正确的情况）：
#   1. UseMySql 单行嵌套（AddDbContext 后直接跟单行 UseMySql，无 { } 块）
#   2. Seeding（启动时查 .Any() 或 EnsureCreated）
#   3. 前端直连服务端口（必须走 Gateway localhost:5000）
#   4. app.Run 硬编码端口（仅当与 UseUrls 不一致时禁止）
#      - 如果 UseUrls 已正确配置，app.Run() 无参数可接受
#      - 如果没有 UseUrls 且硬编码端口（非 5000/5001 等标准端口）→ 违规
# =============================================================================

PROJECT_ROOT="/Users/mac/Projects/WO-Property-Management/src"
FRONTEND_ROOT="/Users/mac/Projects/WO-Property-Management/src/admin-portal/src"
CHECK_FAILED=0

RED='\033[0;31m'
GREEN='\033[0;32m'
NC='\033[0m'

echo "============================================"
echo "WO-Property 编译前规范检查"
echo "时间: $(date '+%Y-%m-%d %H:%M:%S')"
echo "============================================"

# -----------------------------------------------------------------------------
# 检查1：UseMySql 单行嵌套（AddDbContext 后直接跟单行，无 { } 块）
# -----------------------------------------------------------------------------
echo ""
echo -n "[检查1] UseMySql block-style 检查... "

VIOLATIONS=""
for file in "$PROJECT_ROOT"/WO.Property.*/Program.cs; do
    svc=$(basename $(dirname "$file"))
    LINE_NUM=$(grep -n "AddDbContext" "$file" | head -1 | cut -d: -f1)
    if [ -z "$LINE_NUM" ]; then
        continue
    fi
    # 看 AddDbContext 行和接下来1行
    START=$LINE_NUM
    END=$((LINE_NUM + 1))
    CONTEXT=$(sed -n "${START},${END}p" "$file")
    # 如果 AddDbContext( 那行以 ) 结尾（不是以 { 结尾）→ 单行违规
    if echo "$CONTEXT" | head -1 | grep -q "AddDbContext.*)$"; then
        VIOLATIONS="${VIOLATIONS}${svc}: 第${LINE_NUM}行 AddDbContext 单行写法（无 { } 块）\n"
    fi
done

if [ -n "$VIOLATIONS" ]; then
    echo -e "${RED}违规${NC}"
    echo -e "$VIOLATIONS"
    CHECK_FAILED=1
else
    echo -e "${GREEN}通过${NC}"
fi

# -----------------------------------------------------------------------------
# 检查2：数据库 Seeding
# -----------------------------------------------------------------------------
echo ""
echo -n "[检查2] 数据库 Seeding 检查... "

SEEDING_ANY=$(grep -rn "dbContext\.\w+\.Any()" "$PROJECT_ROOT"/WO.Property.*/Program.cs 2>/dev/null || true)
SEEDING_CREATED=$(grep -rn "EnsureCreated()" "$PROJECT_ROOT"/WO.Property.*/Program.cs 2>/dev/null || true)

if [ -n "$SEEDING_ANY" ] || [ -n "$SEEDING_CREATED" ]; then
    echo -e "${RED}违规${NC}"
    [ -n "$SEEDING_ANY" ] && echo "启动查表: $SEEDING_ANY"
    [ -n "$SEEDING_CREATED" ] && echo "EnsureCreated: $SEEDING_CREATED"
    CHECK_FAILED=1
else
    echo -e "${GREEN}通过${NC}"
fi

# -----------------------------------------------------------------------------
# 检查3：前端 API 直连服务端口（禁止前端直接调服务端口）
# -----------------------------------------------------------------------------
echo ""
echo -n "[检查3] 前端 API 直连服务端口检查... "

FRONTEND_DIRECT=$(grep -rn "localhost:500[0-9]\|localhost:501[0-9]" "$FRONTEND_ROOT"/api/*.ts 2>/dev/null | grep -v "localhost:5000" || true)
if [ -n "$FRONTEND_DIRECT" ]; then
    echo -e "${RED}违规${NC}"
    echo "$FRONTEND_DIRECT"
    CHECK_FAILED=1
else
    echo -e "${GREEN}通过${NC}"
fi

# -----------------------------------------------------------------------------
# 检查4：app.Run 硬编码端口（仅当没有 UseUrls 时才检查）
# 如果服务有 UseUrls(config["Urls:Service"])，app.Run() 无参数可接受
# 如果既没有 UseUrls 也没有 config，app.Run("http://...") 硬编码 → 违规
# -----------------------------------------------------------------------------
echo ""
echo -n "[检查4] app.Run 硬编码端口检查... "

for file in "$PROJECT_ROOT"/WO.Property.*/Program.cs; do
    svc=$(basename $(dirname "$file"))
    # 找 UseUrls（是否从配置读取）
    HAS_USEURLS_CONFIG=$(grep -n "UseUrls.*Configuration\|UseUrls.*config\[" "$file" 2>/dev/null || true)
    # 找 app.Run("http://0.0.0.0:PORT")
    APP_RUN_HARDCODED=$(grep -n 'app\.Run("http://0\.0\.0\.0:[0-9]' "$file" 2>/dev/null || true)
    if [ -n "$APP_RUN_HARDCODED" ] && [ -z "$HAS_USEURLS_CONFIG" ]; then
        echo -e "${RED}违规${NC}"
        echo "$svc: $APP_RUN_HARDCODED（无 UseUrls 配置）"
        CHECK_FAILED=1
    fi
done

if [ $CHECK_FAILED -eq 0 ]; then
    echo -e "${GREEN}通过${NC}"
fi

# -----------------------------------------------------------------------------
# 汇总结果
# -----------------------------------------------------------------------------
echo ""
echo "============================================"
if [ "$CHECK_FAILED" -eq 1 ]; then
    echo -e "${RED}❌ 检查失败：发现违规项${NC}"
    exit 1
else
    echo -e "${GREEN}✅ 全部检查通过${NC}"
    exit 0
fi