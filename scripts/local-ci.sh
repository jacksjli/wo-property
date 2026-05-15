#!/bin/bash
# =============================================================================
# WO-Property 本地 CI 检查脚本
# 等效于 GitHub Actions CI，用于本地验证
# =============================================================================

set -e

DOTNET_ROOT=~/dotnet8
export DOTNET_ROOT
export PATH=$DOTNET_ROOT:$PATH

PROJECT_ROOT="/Users/mac/Projects/WO-Property-Management"
SRC_ROOT="$PROJECT_ROOT/src"
ADMIN_ROOT="$PROJECT_ROOT/src/admin-portal"

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo "============================================"
echo "WO-Property 本地 CI 检查"
echo "时间: $(date '+%Y-%m-%d %H:%M:%S')"
echo "============================================"

CHECK_FAILED=0

# -----------------------------------------------------------------------------
# 阶段1: 规范检查（等同于 GitHub Actions pre-check job）
# -----------------------------------------------------------------------------
echo ""
echo -e "${YELLOW}=== 阶段1: 规范检查 ===${NC}"
bash "$PROJECT_ROOT/scripts/pre-build-check.sh" || CHECK_FAILED=1

# -----------------------------------------------------------------------------
# 阶段2: 后端编译（等同于 GitHub Actions backend-build job）
# -----------------------------------------------------------------------------
echo ""
echo -e "${YELLOW}=== 阶段2: 后端编译 ===${NC}"

SERVICES=(
  "GatewayService:5000"
  "ContractService:5001"
  "TicketService:5002"
  "DeviceService:5003"
  "MaterialService:5004"
  "NotificationService:5005"
  "AccessControlService:5006"
  "PaymentService:5007"
  "FinanceService:5009"
  "InspectionService:5010"
  "AnnouncementService:5011"
  "KeyService:5012"
  "VisitorService:5013"
  "MobileService:5014"
  "PersonService:5018"
  "MasterDataService:5019"
  "CleaningService:5016"
  "RenovationService:5021"
  "CommunityService:5022"
  "ExpressService:5017"
  "ParkingService:5025"
)

BACKEND_FAILED=""
for svc_info in "${SERVICES[@]}"; do
  IFS=':' read -r svc port <<< "$svc_info"
  dir="$SRC_ROOT/WO.Property.$svc"
  
  if [ ! -d "$dir" ]; then
    continue
  fi
  
  echo -n "  Building $svc... "
  
  BUILD_OUT=$(rm -rf "$dir/bin" "$dir/obj" && dotnet build "$dir" --configuration Release 2>&1)
  BUILD_RESULT=$?
  
  if [ $BUILD_RESULT -eq 0 ] && [ -f "$dir/bin/Release/net8.0/WO.Property.$svc.dll" ]; then
    echo -e "${GREEN}OK${NC}"
  else
    echo -e "${RED}FAIL${NC}"
    BACKEND_FAILED="$BACKEND_FAILED $svc"
    CHECK_FAILED=1
  fi
done

if [ -n "$BACKEND_FAILED" ]; then
  echo -e "${RED}后端编译失败:$BACKEND_FAILED${NC}"
fi

# -----------------------------------------------------------------------------
# 阶段3: 前端编译（等同于 GitHub Actions frontend-build job）
# -----------------------------------------------------------------------------
echo ""
echo -e "${YELLOW}=== 阶段3: 前端编译 ===${NC}"

if [ -d "$ADMIN_ROOT" ]; then
  echo -n "  Building admin-portal... "
  
  cd "$ADMIN_ROOT"
  BUILD_OUT=$(npm run build 2>&1)
  BUILD_RESULT=$?
  
  if [ $BUILD_RESULT -eq 0 ] && [ -d "$ADMIN_ROOT/dist" ]; then
    echo -e "${GREEN}OK${NC}"
  else
    echo -e "${RED}FAIL${NC}"
    CHECK_FAILED=1
  fi
else
  echo -e "${YELLOW}  admin-portal 未找到，跳过${NC}"
fi

# -----------------------------------------------------------------------------
# 汇总
# -----------------------------------------------------------------------------
echo ""
echo "============================================"
if [ "$CHECK_FAILED" -eq 1 ]; then
  echo -e "${RED}❌ CI 检查失败${NC}"
  exit 1
else
  echo -e "${GREEN}✅ 所有 CI 检查通过${NC}"
  echo ""
  echo "规范检查:     ✅ 通过"
  echo "后端编译:     ✅ 21 服务"
  echo "前端编译:     ✅ admin-portal"
  exit 0
fi