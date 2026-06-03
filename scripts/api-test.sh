#!/bin/bash
# WO Property - 完整 API 链路测试

TOKEN=$(curl -s -X POST http://localhost:5106/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Admin@123"}' | grep -o '"token":"[^"]*"' | cut -d'"' -f4)

echo "=== Step 1: 认证Token获取 ==="
if [ -n "$TOKEN" ]; then
    echo "✅ Token获取成功: ${TOKEN:0:40}..."
else
    echo "❌ Token获取失败"
    exit 1
fi

echo ""
echo "=== Step 2: 各服务API直接调用测试 ==="
echo ""

test_api() {
    name=$1
    port=$2
    path=$3
    
    response=$(curl -s --max-time 5 -w "|||%{http_code}" -H "Authorization: Bearer $TOKEN" "http://localhost:$port$path" 2>/dev/null)
    http_code=$(echo "$response" | grep -o '|||.*' | tr -d '|||')
    body=$(echo "$response" | sed 's/|||.*//' | head -c 150)
    
    if [ "$http_code" = "200" ]; then
        echo "✅ $name ($port$path) → 200 | $body"
    elif [ "$http_code" = "401" ]; then
        echo "⚠️  $name ($port$path) → 401 未授权 | $body"
    elif [ "$http_code" = "404" ]; then
        echo "⚠️  $name ($port$path) → 404 路由不存在 | $body"
    elif [ "$http_code" = "000" ]; then
        echo "❌ $name ($port$path) → 连接失败"
    else
        echo "🔶 $name ($port$path) → $http_code | $body"
    fi
}

# 16个服务的核心API测试
test_api "PersonService"      5018 "/api/persons"
test_api "MasterDataService"  5019 "/api/fields"
test_api "TicketService"      5102 "/api/tenant/tickets"
test_api "DispatchService"    5241 "/api/dispatch/rules"
test_api "MaterialService"    5504 "/api/materials"
test_api "NotificationService" 5005 "/api/notifications"
test_api "DeviceService"      5530 "/api/devices"
test_api "ContractService"    5501 "/api/contracts"
test_api "FinanceService"     5509 "/api/bills"
test_api "InspectionService"   5510 "/api/inspections"
test_api "ComplaintService"   5011 "/api/complaints"
test_api "KeyService"         5512 "/api/keys"
test_api "VisitorService"     5513 "/api/visitors"
test_api "StatisticsService"  5250 "/api/stats"
test_api "MobileService"      5526 "/api/mobile/config"

echo ""
echo "=== Step 3: 通过 Gateway 测试 ==="
echo ""

test_api "Gateway→PersonService" 5000 "/api/persons"
test_api "Gateway→MasterData"   5000 "/api/fields"
test_api "Gateway→Ticket"      5000 "/api/tickets"
test_api "Gateway→Dispatch"    5000 "/api/dispatch/rules"

echo ""
echo "=== 测试完成: $(date '+%Y-%m-%d %H:%M:%S') ==="