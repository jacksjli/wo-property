#!/bin/bash
# WO 物业管理服务回归测试
# 用途：验证服务在 watchdog 重启后能正常启动，用于防止类似 dotnet PATH 问题再次发生
# 使用：bash scripts/test_gw_restart.sh

set -e

SCRIPT_DIR="/Users/mac/Projects/WO-Property-Management"
LOG_FILE="$SCRIPT_DIR/logs/test-regression.log"

log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1"
}

log "=== WO 物业管理服务回归测试 ==="

# 测试函数
test_service() {
    local name=$1
    local port=$2
    local retries=3
    
    for i in $(seq 1 $retries); do
        result=$(curl -s --connect-timeout 3 "http://localhost:$port/health" 2>/dev/null | python3 -c "import sys,json; print(json.load(sys.stdin)['service'])" 2>/dev/null || echo "DOWN")
        if [ "$result" != "DOWN" ]; then
            log "✅ $name (端口 $port): OK"
            return 0
        fi
        sleep 2
    done
    log "❌ $name (端口 $port): DOWN after $retries retries"
    return 1
}

# 关键服务列表
SERVICES=(
    "GatewayService:5000"
    "AuthService:5106"
    "TicketService:5102"
    "DispatchService:5241"
    "PersonService:5018"
    "MasterDataService:5019"
)

FAILED=0
for svc in "${SERVICES[@]}"; do
    name="${svc%%:*}"
    port="${svc##*:}"
    if ! test_service "$name" "$port"; then
        FAILED=1
    fi
done

if [ $FAILED -eq 0 ]; then
    log "=== ✅ 所有关键服务在线 ==="
    exit 0
else
    log "=== ❌ 部分服务异常 ==="
    exit 1
fi