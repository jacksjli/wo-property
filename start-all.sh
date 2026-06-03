#!/bin/bash
# WO Property Management - 启动所有服务
# 用法: bash start-all.sh
# 说明: 启动前自动检测本机IP并更新所有配置文件
# 最后更新: 2026-06-02

set -e
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
LOG_DIR="$SCRIPT_DIR/logs"
PROJ="$SCRIPT_DIR/src"
IP_FILE="$SCRIPT_DIR/.server-ip"

mkdir -p "$LOG_DIR"

echo "=========================================="
echo "WO 物业管理服务"
echo "=========================================="

# ========== 自动检测并更新IP配置 ==========
echo ""
echo "[1/4] 检测本机局域网IP..."
DETECTED_IP=""
for iface in en0 en1 en2; do
  IP=$(ifconfig $iface 2>/dev/null | grep "inet " | awk '{print $2}' | head -1)
  if [ -n "$IP" ] && [ "$IP" != "127.0.0.1" ]; then
    DETECTED_IP="$IP"
    break
  fi
done

if [ -z "$DETECTED_IP" ]; then
  echo "  ❌ 无法检测局域网IP，请检查网络连接"
  exit 1
fi
echo "  本机IP: $DETECTED_IP"

# 读取上次配置的IP
LAST_IP=""
if [ -f "$IP_FILE" ]; then
  LAST_IP=$(cat "$IP_FILE")
fi

echo ""
echo "[2/4] 更新配置文件..."

# 如果IP没变化，跳过更新
if [ "$DETECTED_IP" = "$LAST_IP" ]; then
  echo "  IP 无变化，跳过配置更新"
else
  echo "  IP 变化: ${LAST_IP:-无} → $DETECTED_IP"
  
  # 1. 更新 admin-portal src/api/*.ts 和 src/stores/*.ts
  ADMIN_DIR="$SCRIPT_DIR/src/admin-portal"
  if [ -d "$ADMIN_DIR" ]; then
    for f in $(find "$ADMIN_DIR/src/api" "$ADMIN_DIR/src/stores" -name "*.ts" 2>/dev/null); do
      if grep -qE "192\.168\.[0-9]+\.[0-9]+" "$f" 2>/dev/null; then
        sed -i '' -E "s/192\.168\.[0-9]+\.[0-9]+/$DETECTED_IP/g" "$f"
        echo "  ✅ $(basename $f)"
      fi
    done
  fi

  # 2. 更新小程序 env.js
  MINI_ENV="$SCRIPT_DIR/src/woa-property-mini/src/config/env.js"
  if [ -f "$MINI_ENV" ]; then
    sed -i '' -E "s/192\.168\.[0-9]+\.[0-9]+/$DETECTED_IP/g" "$MINI_ENV"
    echo "  ✅ woa-property-mini/config/env.js"
  fi

  # 3. 更新小程序 App.vue（如果也有 IP）
  MINI_APP="$SCRIPT_DIR/src/woa-property-mini/src/App.vue"
  if [ -f "$MINI_APP" ]; then
    if grep -qE "192\.168\.[0-9]+\.[0-9]+" "$MINI_APP" 2>/dev/null; then
      sed -i '' -E "s/192\.168\.[0-9]+\.[0-9]+/$DETECTED_IP/g" "$MINI_APP"
      echo "  ✅ App.vue"
    fi
  fi
  
  # 4. 更新后端服务 CORS 配置（appsettings.json）
  for svc in GatewayService AuthService TicketService DispatchService PersonService MasterDataService; do
    cfg="$SCRIPT_DIR/src/WO.Property.$svc/appsettings.json"
    if [ -f "$cfg" ]; then
      if grep -qE "192\.168\.[0-9]+\.[0-9]+" "$cfg" 2>/dev/null; then
        sed -i '' -E "s/192\.168\.[0-9]+\.[0-9]+/$DETECTED_IP/g" "$cfg"
        echo "  ✅ $svc/appsettings.json"
      fi
    fi
  done
  
  # 5. 更新 MySQL 配置（如果需要）
  MYSQL_CNF="/usr/local/etc/my.cnf"
  if [ -f "$MYSQL_CNF" ]; then
    if grep -qE "bind-address" "$MYSQL_CNF" 2>/dev/null; then
      sed -i '' -E "s/bind-address\s*=\s*.*/bind-address = 0.0.0.0/g" "$MYSQL_CNF"
      echo "  ✅ MySQL my.cnf"
    fi
  fi
  
  echo "  ✅ IP 配置已全部更新"
fi

# 保存当前IP
echo "$DETECTED_IP" > "$IP_FILE"

# ========== 启动后端微服务 ==========
echo ""
echo "[3/4] 启动后端微服务..."

start_service() {
  local name=$1
  local port=$2
  local project=$3
  local log="$LOG_DIR/$(echo "$name" | tr '[:upper:]' '[:lower:]').log"

  if lsof -i :$port -s TCP:LISTEN -t > /dev/null 2>&1; then
    echo "  $name (端口 $port) 已运行，跳过"
  else
    echo "  启动 $name (端口 $port)..."
    nohup /usr/local/share/dotnet/dotnet run -c Release --project "$PROJ/$project" -- --urls="http://0.0.0.0:$port" > "$log" 2>&1 &
    echo "    PID: $!"
  fi
}

start_service "GatewayService" 5000 "WO.Property.GatewayService/WO.Property.GatewayService.csproj"
start_service "AuthService" 5106 "WO.Property.AuthService/WO.Property.AuthService.csproj"
start_service "TicketService" 5102 "WO.Property.TicketService/WO.Property.TicketService.csproj"
start_service "DispatchService" 5241 "WO.Property.DispatchService/WO.Property.DispatchService.csproj"
start_service "PersonService" 5018 "WO.Property.PersonService/WO.Property.PersonService.csproj"
start_service "MasterDataService" 5019 "WO.Property.MasterDataService/WO.Property.MasterDataService.csproj"
start_service "MaterialService" 5504 "WO.Property.MaterialService/WO.Property.MaterialService.csproj"
start_service "NotificationService" 5105 "WO.Property.NotificationService/WO.Property.NotificationService.csproj"
start_service "PaymentService" 5109 "WO.Property.PaymentService/WO.Property.PaymentService.csproj"
start_service "DeviceService" 5530 "WO.Property.DeviceService/WO.Property.DeviceService.csproj"
start_service "ContractService" 5501 "WO.Property.ContractService/WO.Property.ContractService.csproj"
start_service "FinanceService" 5509 "WO.Property.FinanceService/WO.Property.FinanceService.csproj"
start_service "InspectionService" 5510 "WO.Property.InspectionService/WO.Property.InspectionService.csproj"
start_service "ComplaintService" 5201 "WO.Property.ComplaintService/WO.Property.ComplaintService.csproj"
start_service "KeyService" 5512 "WO.Property.KeyService/WO.Property.KeyService.csproj"
start_service "VisitorService" 5513 "WO.Property.VisitorService/WO.Property.VisitorService.csproj"
start_service "StatisticsService" 5250 "WO.Property.StatisticsService/WO.Property.StatisticsService.csproj"
start_service "MobileService" 5526 "WO.Property.MobileService/WO.Property.MobileService.csproj"
start_service "CommunityService" 5522 "WO.Property.CommunityService/WO.Property.CommunityService.csproj"
start_service "ParkingService" 5525 "WO.Property.ParkingService/WO.Property.ParkingService.csproj"
start_service "RenovationService" 5521 "WO.Property.RenovationService/WO.Property.RenovationService.csproj"

# admin-portal 前端
cd "$PROJ/admin-portal"
if lsof -i :5173 -s TCP:LISTEN -t > /dev/null 2>&1; then
  echo "  admin-portal (端口 5173) 已运行，跳过"
else
  echo "  启动 admin-portal (端口 5173)..."
  nohup npm run dev > "$LOG_DIR/admin.log" 2>&1 &
  echo "    PID: $!"
fi
cd "$SCRIPT_DIR"

echo ""
echo "[4/4] 等待服务启动（15秒）..."
sleep 30

echo ""
echo "服务健康检查："
ALL_OK=true
for port in 5000 5106 5102 5241 5018 5019 5504 5105 5109 5530 5501 5509 5510 5201 5512 5513 5250 5526 5522 5525 5521; do
  if curl -s --connect-timeout 3 "http://localhost:$port/health" > /dev/null 2>&1; then
    echo "  端口 $port: ✅"
  else
    echo "  端口 $port: ❌"
    ALL_OK=false
  fi
done

if lsof -i :5173 -s TCP:LISTEN -t > /dev/null 2>&1; then
  echo "  端口 5173 (admin): ✅"
else
  echo "  端口 5173 (admin): ❌"
  ALL_OK=false
fi

echo ""
echo "=========================================="
echo "✅ 启动完成！本机IP: $DETECTED_IP"
echo "=========================================="
echo ""
echo "服务地址："
echo "  后端 Gateway:   http://localhost:5000"
echo "  管理后台:       http://localhost:5173"
echo "  手机调试地址:   http://$DETECTED_IP:5173"
echo ""
echo "配置文件: $IP_FILE"
echo "日志位置: $LOG_DIR/"