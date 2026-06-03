#!/bin/bash
# =============================================================================
# WO-Property 本地 CI 检查脚本
# 等效于 GitHub Actions CI，用于本地验证
# =============================================================================
# 最后更新: 2026-06-02

PROJECT_ROOT="/Users/mac/Projects/WO-Property-Management"
SRC_ROOT="$PROJECT_ROOT/src"

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo "============================================"
echo "WO-Property 本地 CI 检查"
echo "时间: $(date '+%Y-%m-%d %H:%M:%S')"
echo "============================================"

FAILED=""

# -----------------------------------------------------------------------------
# 阶段1: 规范检查
# -----------------------------------------------------------------------------
echo ""
echo -e "${YELLOW}=== 阶段1: 规范检查 ===${NC}"
CHECK_OUTPUT=$(bash "$PROJECT_ROOT/scripts/pre-build-check.sh" 2>&1)
if echo "$CHECK_OUTPUT" | grep -qE "❌|违反|违规"; then
  echo -e "${RED}  规范检查有违规${NC}"
  FAILED="$FAILED pre-check"
else
  echo -e "${GREEN}  规范检查通过${NC}"
fi

# -----------------------------------------------------------------------------
# 阶段2: 后端编译（只编译当前真实使用的 6 个服务）
# -----------------------------------------------------------------------------
echo ""
echo -e "${YELLOW}=== 阶段2: 后端编译 ===${NC}"

SERVICES=(
  "GatewayService"
  "AuthService"
  "TicketService"
  "DispatchService"
  "PersonService"
  "MasterDataService"
)

for svc in "${SERVICES[@]}"; do
  csproj="$SRC_ROOT/WO.Property.$svc/WO.Property.$svc.csproj"
  if [ ! -f "$csproj" ]; then
    echo -e "  ${YELLOW}$svc: 跳过（无 csproj）${NC}"
    continue
  fi
  echo -n "  编译 $svc... "
  BUILD_OUTPUT=$(dotnet build "$csproj" -c Release 2>&1)
  if echo "$BUILD_OUTPUT" | grep -qE "已成功生成|Build succeeded"; then
    echo -e "${GREEN}✅${NC}"
  else
    echo -e "${RED}❌${NC}"
    echo "    $(echo "$BUILD_OUTPUT" | grep -E "error|Error" | head -2)"
    FAILED="$FAILED $svc"
  fi
done

# -----------------------------------------------------------------------------
# 阶段3: 前端编译
# -----------------------------------------------------------------------------
echo ""
echo -e "${YELLOW}=== 阶段3: 前端编译 ===${NC}"
ADMIN_ROOT="$PROJECT_ROOT/src/admin-portal"
if [ -d "$ADMIN_ROOT" ]; then
  echo -n "  admin-portal... "
  cd "$ADMIN_ROOT"
  BUILD_OUTPUT=$(npm run build 2>&1)
  if echo "$BUILD_OUTPUT" | grep -qE "built|success|完成|DONE|dist"; then
    echo -e "${GREEN}✅${NC}"
  else
    echo -e "${RED}❌${NC}"
    FAILED="$FAILED admin-portal"
  fi
  cd "$PROJECT_ROOT"
else
  echo "  admin-portal: 跳过（无目录）"
fi

# -----------------------------------------------------------------------------
# 阶段4: 小程序编译
# -----------------------------------------------------------------------------
echo ""
echo -e "${YELLOW}=== 阶段4: 小程序编译 ===${NC}"
MINI_ROOT="$PROJECT_ROOT/src/woa-property-mini"
if [ -d "$MINI_ROOT" ]; then
  echo -n "  woa-property-mini... "
  cd "$MINI_ROOT"
  BUILD_OUTPUT=$(npm run build:mp-weixin 2>&1)
  if echo "$BUILD_OUTPUT" | grep -qE "DONE|complete|成功|success"; then
    echo -e "${GREEN}✅${NC}"
  else
    echo -e "${RED}❌${NC}"
    FAILED="$FAILED woa-property-mini"
  fi
  cd "$PROJECT_ROOT"
else
  echo "  woa-property-mini: 跳过（无目录）"
fi

# -----------------------------------------------------------------------------
# 汇总
# -----------------------------------------------------------------------------
echo ""
echo "============================================"
if [ -n "$FAILED" ]; then
  echo -e "${RED}❌ CI 失败${NC}"
  echo "  失败项:$FAILED"
  exit 1
else
  echo -e "${GREEN}✅ CI 全部通过${NC}"
fi
echo "============================================"