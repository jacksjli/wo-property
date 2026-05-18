#!/bin/bash
# Phase 0 Multi-Tenant API Integration Test Suite
# 用 curl 验证租户隔离，不需要 NuGet 包

AUTH_SERVICE="http://127.0.0.1:5106"
TICKET_SERVICE="http://127.0.0.1:5102"

PASS=0
FAIL=0

echo "=========================================="
echo "  Phase 0 多租户 API 集成测试"
echo "=========================================="
echo ""

pass() { echo "  ✅ $1"; ((PASS++)); }
fail() { echo "  ❌ $1"; ((FAIL++)); }

# Helper: check if service is up
check_service() {
    local url=$1 local name=$2
    curl -s --connect-timeout 2 --max-time 3 "$url/health" > /dev/null 2>&1 && pass "$name UP" || fail "$name DOWN"
}

# Helper: login and extract token (returns empty if fails)
login() {
    local username=$1 local password=$2 local tenant=$3
    curl -s -X POST "$AUTH_SERVICE/api/auth/login" \
        -H "Content-Type: application/json" \
        -d "{\"username\":\"$username\",\"password\":\"$password\",\"tenantCode\":\"$tenant\"}" | \
        python3 -c "import sys,json; d=json.load(sys.stdin); print(d['data']['token'] if d.get('success') else '')" 2>/dev/null
}

# Helper: HTTP GET with token
api_get() {
    local url=$1 local token=$2
    curl -s "$url" -H "Authorization: Bearer $token"
}

# Helper: HTTP POST with token and body
api_post() {
    local url=$1 local token=$2 local body=$3
    curl -s -X POST "$url" -H "Content-Type: application/json" -H "Authorization: Bearer $token" -d "$body"
}

# Helper: HTTP PUT with token and body
api_put() {
    local url=$1 local token=$2 local body=$3
    curl -s -X PUT "$url" -H "Content-Type: application/json" -H "Authorization: Bearer $token" -d "$body"
}

# Helper: HTTP DELETE with token
api_del() {
    local url=$1 local token=$2
    curl -s -X DELETE "$url" -H "Authorization: Bearer $token"
}

# Helper: extract field from JSON response
get_field() {
    echo "$1" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d.get('$2',''))" 2>/dev/null
}

get_data_field() {
    echo "$1" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d.get('data',{}).get('$2',''))" 2>/dev/null
}

# =============================================
# 1. Health Check
# =============================================
echo "--- 1. 服务健康检查 ---"
check_service "$AUTH_SERVICE" "AuthService (5106)"
check_service "$TICKET_SERVICE" "TicketService (5102)"
echo ""

# =============================================
# 2. Login Tests (AuthService)
# =============================================
echo "--- 2. 登录测试 ---"
TOKEN_A=$(login "tech_a" "Test@123" "tenant_a")
[ -n "$TOKEN_A" ] && pass "tenant_a 登录" || { fail "tenant_a 登录"; TOKEN_A=""; }

TOKEN_B=$(login "admin_b" "Test@123" "tenant_b")
[ -n "$TOKEN_B" ] && pass "tenant_b 登录" || { fail "tenant_b 登录"; TOKEN_B=""; }

# tech_a is also in tenant_a
TOKEN_TECH_A=$(login "tech_a" "Test@123" "tenant_a")
[ -n "$TOKEN_TECH_A" ] && pass "tech_a (tenant_a) 登录" || fail "tech_a 登录"
echo ""

# =============================================
# 3. Tenant Isolation - CRUD
# =============================================
echo "--- 3. 租户隔离 CRUD ---"

if [ -z "$TOKEN_A" ] || [ -z "$TOKEN_B" ]; then
    echo "  ⚠️  Token获取失败，跳过CRUD测试"
else
    # Create ticket for tenant_a
    NEW_A=$(api_post "$TICKET_SERVICE/api/tenant/tickets" "$TOKEN_A" \
        '{"title":"集成测试-A","description":"验证隔离","category":"设备维修","priority":"High","projectId":1}')
    SUCCESS_A=$(echo "$NEW_A" | python3 -c "import sys,json; print(json.load(sys.stdin).get('success',False))" 2>/dev/null)
    [ "$SUCCESS_A" = "True" ] && pass "tenant_a 创建工单" || fail "tenant_a 创建工单"
    ID_A=$(echo "$NEW_A" | python3 -c "import sys,json; print(json.load(sys.stdin).get('data',{}).get('id',''))" 2>/dev/null)

    # Create ticket for tenant_b
    NEW_B=$(api_post "$TICKET_SERVICE/api/tenant/tickets" "$TOKEN_B" \
        '{"title":"集成测试-B","description":"验证隔离","category":"保洁","priority":"Medium","projectId":3}')
    SUCCESS_B=$(echo "$NEW_B" | python3 -c "import sys,json; print(json.load(sys.stdin).get('success',False))" 2>/dev/null)
    [ "$SUCCESS_B" = "True" ] && pass "tenant_b 创建工单" || fail "tenant_b 创建工单"
    ID_B=$(echo "$NEW_B" | python3 -c "import sys,json; print(json.load(sys.stdin).get('data',{}).get('id',''))" 2>/dev/null)

    # Query tenant_a - should NOT see tenant_b's ticket
    Q_A=$(api_get "$TICKET_SERVICE/api/tenant/tickets" "$TOKEN_A")
    TOT_A=$(echo "$Q_A" | python3 -c "import sys,json; print(json.load(sys.stdin).get('total',0))" 2>/dev/null)
    [ "$TOT_A" -gt 0 ] && pass "tenant_a 查询到 $TOT_A 条工单" || fail "tenant_a 查询失败"
    TITLES_A=$(echo "$Q_A" | python3 -c "import sys,json; print(json.load(sys.stdin).get('data',[]))" 2>/dev/null)
    if echo "$TITLES_A" | grep -q "集成测试-B"; then
        fail "tenant_a 看到了 tenant_b 的工单（隔离失败）"
    else
        pass "tenant_a 看不到 tenant_b 的工单"
    fi

    # Query tenant_b - should NOT see tenant_a's ticket
    Q_B=$(api_get "$TICKET_SERVICE/api/tenant/tickets" "$TOKEN_B")
    TOT_B=$(echo "$Q_B" | python3 -c "import sys,json; print(json.load(sys.stdin).get('total',0))" 2>/dev/null)
    [ "$TOT_B" -gt 0 ] && pass "tenant_b 查询到 $TOT_B 条工单" || fail "tenant_b 查询失败"
    TITLES_B=$(echo "$Q_B" | python3 -c "import sys,json; print(json.load(sys.stdin).get('data',[]))" 2>/dev/null)
    if echo "$TITLES_B" | grep -q "集成测试-A"; then
        fail "tenant_b 看到了 tenant_a 的工单（隔离失败）"
    else
        pass "tenant_b 看不到 tenant_a 的工单"
    fi

    # Update ticket (tenant_a updates its own)
    if [ -n "$ID_A" ]; then
        UP_A=$(api_put "$TICKET_SERVICE/api/tenant/tickets/$ID_A" "$TOKEN_A" '{"status":"Processing","priority":"Low"}')
        ST=$(echo "$UP_A" | python3 -c "import sys,json; print(json.load(sys.stdin).get('data',{}).get('status',''))" 2>/dev/null)
        [ "$ST" = "Processing" ] && pass "tenant_a 更新工单状态=Processing" || fail "tenant_a 更新工单"
    fi

    # Delete ticket (tenant_a deletes its own)
    if [ -n "$ID_A" ]; then
        DEL_A=$(api_del "$TICKET_SERVICE/api/tenant/tickets/$ID_A" "$TOKEN_A")
        OK=$(echo "$DEL_A" | python3 -c "import sys,json; print(json.load(sys.stdin).get('success',False))" 2>/dev/null)
        [ "$OK" = "True" ] && pass "tenant_a 删除工单" || fail "tenant_a 删除工单"
    fi
fi
echo ""

# =============================================
# 4. Multi-project within tenant
# =============================================
echo "--- 4. 同租户多项目数据隔离 ---"
if [ -n "$TOKEN_TECH_A" ]; then
    # tech_a has access to projectIds [1,2] - create ticket for project 2
    NEW_A2=$(api_post "$TICKET_SERVICE/api/tenant/tickets" "$TOKEN_TECH_A" \
        '{"title":"tech_a-项目2工单","description":"测试多项目","category":"保洁","priority":"Low","projectId":2}')
    OK=$(echo "$NEW_A2" | python3 -c "import sys,json; print(json.load(sys.stdin).get('success',False))" 2>/dev/null)
    [ "$OK" = "True" ] && pass "tech_a 为项目2创建工单" || fail "tech_a 项目2工单"
else
    echo "  ⚠️  tech_a token 不可用，跳过"
fi
echo ""

# =============================================
# 5. Database-level isolation
# =============================================
echo "--- 5. 数据库层面隔离 ---"
if command -v mysql &> /dev/null; then
    A_COUNT=$(mysql -h 127.0.0.1 -P 3306 -u root -e "USE tenant_a; SELECT COUNT(*) FROM tickets;" 2>/dev/null | tail -1)
    B_COUNT=$(mysql -h 127.0.0.1 -P 3306 -u root -e "USE tenant_b; SELECT COUNT(*) FROM tickets;" 2>/dev/null | tail -1)
    [ -n "$A_COUNT" ] && pass "tenant_a.tickets 有 $A_COUNT 条" || echo "  ⚠️  mysql 查询失败"
    [ -n "$B_COUNT" ] && pass "tenant_b.tickets 有 $B_COUNT 条" || echo "  ⚠️  mysql 查询失败"
else
    echo "  ⚠️  mysql 不可用，跳过"
fi
echo ""

# =============================================
# Summary
# =============================================
echo "=========================================="
echo "  测试结果: $PASS 通过, $FAIL 失败"
echo "=========================================="
[ $FAIL -eq 0 ] && echo "✅ 全部测试通过！" || echo "❌ 有测试失败"
exit $FAIL