#!/bin/bash
# WO 物业管理服务健康检查与自动重启脚本
# 用法: bash scripts/service-watchdog.sh

SCRIPT_DIR="/Users/mac/Projects/WO-Property-Management"
LOG_FILE="$SCRIPT_DIR/logs/service-watchdog.log"
CRASH_DIR="/tmp/service-crashes"
PORTS=(5000 5106 5102 5241 5018 5019)
SERVICE_NAMES=("GatewayService" "AuthService" "TicketService" "DispatchService" "PersonService" "MasterDataService")

log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1" | tee -a "$LOG_FILE"
}

mkdir -p "$SCRIPT_DIR/logs"
mkdir -p "$CRASH_DIR"

# 记录崩溃次数（5分钟内同一服务崩溃超过阈值则不再自动重启）
check_crash_count() {
    local port=$1
    local name=$2
    local crash_file="$CRASH_DIR/$port"
    local now=$(date +%s)
    local window=300  # 5分钟窗口
    local max_crashes=2

    # 清理超过5分钟的旧记录
    if [ -f "$crash_file" ]; then
        # 保留5分钟内的记录
        touch "$crash_file"
        # 过滤掉超过5分钟的旧时间戳
        grep -v "^[0-9]*$" "$crash_file" 2>/dev/null
        local valid=$(awk -v now="$now" -v window="$window" '$1 > (now - window)' "$crash_file" 2>/dev/null | wc -l)
        if [ "$valid" -gt "$max_crashes" ]; then
            log "⚠️  $name 5分钟内崩溃 $valid 次，人工介入"
            return 1
        fi
    fi
    return 0
}

record_crash() {
    local port=$1
    local crash_file="$CRASH_DIR/$port"
    echo "$(date +%s)" >> "$crash_file"
}

restart_service() {
    local port=$1
    local name=$2
    log "⚠️  $name (端口 $port) 无响应，尝试重启..."

    # 检查是否连续崩溃
    if ! check_crash_count $port "$name"; then
        log "❌ $name 连续崩溃次数过多，已暂停自动重启，请人工检查"
        return
    fi

    # 杀掉占用该端口的进程
    local pid=$(lsof -ti :$port 2>/dev/null)
    if [ -n "$pid" ]; then
        kill -9 $pid 2>/dev/null
        sleep 2
    fi

    # 根据端口启动对应服务
    case $port in
        5000)
            cd "$SCRIPT_DIR/src/WO.Property.GatewayService" && nohup /usr/local/share/dotnet/dotnet run --no-build -c Release -- --urls="http://0.0.0.0:5000" >> "$SCRIPT_DIR/logs/gateway.log" 2>&1 &
            ;;
        5106)
            cd "$SCRIPT_DIR/src/WO.Property.AuthService" && nohup /usr/local/share/dotnet/dotnet run --no-build -c Release -- --urls="http://0.0.0.0:5106" >> "$SCRIPT_DIR/logs/auth.log" 2>&1 &
            ;;
        5102)
            cd "$SCRIPT_DIR/src/WO.Property.TicketService" && nohup /usr/local/share/dotnet/dotnet run --no-build -c Release -- --urls="http://0.0.0.0:5102" >> "$SCRIPT_DIR/logs/ticket.log" 2>&1 &
            ;;
        5241)
            cd "$SCRIPT_DIR/src/WO.Property.DispatchService" && nohup /usr/local/share/dotnet/dotnet run --no-build -c Release -- --urls="http://0.0.0.0:5241" >> "$SCRIPT_DIR/logs/dispatch.log" 2>&1 &
            ;;
        5018)
            cd "$SCRIPT_DIR/src/WO.Property.PersonService" && nohup /usr/local/share/dotnet/dotnet run --no-build -c Release -- --urls="http://0.0.0.0:5018" >> "$SCRIPT_DIR/logs/person.log" 2>&1 &
            ;;
        5019)
            cd "$SCRIPT_DIR/src/WO.Property.MasterDataService" && nohup /usr/local/share/dotnet/dotnet run --no-build -c Release -- --urls="http://0.0.0.0:5019" >> "$SCRIPT_DIR/logs/masterdata.log" 2>&1 &
            ;;
    esac

    sleep 5
    if curl -s --connect-timeout 3 "http://localhost:$port/health" > /dev/null 2>&1; then
        log "✅ $name 重启成功"
        # 成功后清除崩溃计数
        rm -f "$CRASH_DIR/$port"
    else
        log "❌ $name 重启失败，记录崩溃"
        record_crash $port
    fi
}

# 检查所有服务
all_healthy=true
for i in "${!PORTS[@]}"; do
    port=${PORTS[$i]}
    name=${SERVICE_NAMES[$i]}
    if ! curl -s --connect-timeout 3 "http://localhost:$port/health" > /dev/null 2>&1; then
        all_healthy=false
        restart_service $port "$name"
    fi
done

if $all_healthy; then
    log "✅ 所有服务健康 (检查时间: $(date '+%H:%M:%S'))"
fi