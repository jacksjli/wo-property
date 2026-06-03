#!/bin/bash
# WO Property Management - 自动检测本机IP并更新配置
# 用法: bash setup.sh
# 适用场景：在新 Mac 上首次部署，运行 start-all.sh 之前执行

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
echo "=========================================="
echo "WO 物业管理软件 - 自动配置"
echo "=========================================="
echo ""

# ========== 步骤1：检测本机局域网IP ==========
echo "步骤1：检测本机局域网IP..."

DETECTED_IP=""
for iface in en0 en1 en2; do
  IP=$(ifconfig $iface 2>/dev/null | grep "inet " | awk '{print $2}' | head -1)
  if [ -n "$IP" ] && [ "$IP" != "127.0.0.1" ]; then
    DETECTED_IP="$IP"
    echo "  检测到: $iface → $DETECTED_IP"
    break
  fi
done

if [ -z "$DETECTED_IP" ]; then
  echo "  ❌ 无法检测到局域网IP，请检查网络连接"
  echo "  提示：确保 Mac 已连接到局域网（非VPN）"
  exit 1
fi

echo "  本机IP: $DETECTED_IP"
echo ""

# ========== 步骤2：检测 MySQL 是否运行 ==========
echo "步骤2：检测 MySQL..."
MYSQL_RUNNING=false
if mysql -u root -p'' --socket=/tmp/mysql.sock -e "SELECT 1" > /dev/null 2>&1; then
  MYSQL_RUNNING=true
  echo "  ✅ MySQL 运行正常"
elif mysql -u root -p'' -h 127.0.0.1 -e "SELECT 1" > /dev/null 2>&1; then
  MYSQL_RUNNING=true
  echo "  ✅ MySQL 运行正常（TCP）"
else
  echo "  ⚠️  MySQL 未运行，请先启动 MySQL"
fi
echo ""

# ========== 步骤3：检测 .NET SDK ==========
echo "步骤3：检测 .NET SDK..."
DOTNET_VER=$(dotnet --version 2>/dev/null || echo "")
if [ -n "$DOTNET_VER" ]; then
  echo "  ✅ .NET SDK $DOTNET_VER"
else
  echo "  ❌ 未安装 .NET SDK，请先安装"
  exit 1
fi
echo ""

# ========== 步骤4：更新配置文件中的IP ==========
echo "步骤4：更新配置文件中的 IP 地址..."

# 4a. 微信小程序 env.js
ENV_FILE="$SCRIPT_DIR/src/woa-property-mini/src/config/env.js"
if [ -f "$ENV_FILE" ]; then
  sed -i '' "s/192\.168\.[0-9]\+\.[0-9]\+/$DETECTED_IP/g" "$ENV_FILE"
  echo "  ✅ $ENV_FILE"
else
  echo "  ⚠️  $ENV_FILE 不存在，跳过"
fi

# 4b. admin-portal api/config.ts
CONFIG_FILE="$SCRIPT_DIR/src/admin-portal/src/api/config.ts"
if [ -f "$CONFIG_FILE" ]; then
  sed -i '' "s/192\.168\.[0-9]\+\.[0-9]\+/$DETECTED_IP/g" "$CONFIG_FILE"
  echo "  ✅ $CONFIG_FILE"
else
  echo "  ⚠️  $CONFIG_FILE 不存在，跳过"
fi

# 4c. admin-portal websocket.ts
WS_FILE="$SCRIPT_DIR/src/admin-portal/src/stores/websocket.ts"
if [ -f "$WS_FILE" ]; then
  sed -i '' "s/192\.168\.[0-9]\+\.[0-9]\+/$DETECTED_IP/g" "$WS_FILE"
  echo "  ✅ $WS_FILE"
fi

# 4d. admin-portal 其他服务 API（cleaning/payment/contract 等）
for f in "$SCRIPT_DIR/src/admin-portal/src/api"/*.ts; do
  if [ -f "$f" ]; then
    if grep -q "192\.168\.[0-9]\+\.[0-9]\+" "$f" 2>/dev/null; then
      sed -i '' "s/192\.168\.[0-9]\+\.[0-9]\+/$DETECTED_IP/g" "$f"
      echo "  ✅ $(basename $f)"
    fi
  fi
done

echo ""
echo "✅ 配置更新完成！本机 IP: $DETECTED_IP"
echo ""

# ========== 步骤5：创建日志目录 ==========
mkdir -p "$SCRIPT_DIR/logs"
echo "✅ 日志目录已创建: $SCRIPT_DIR/logs"
echo ""

# ========== 步骤6：创建便捷启动快捷命令 ==========
echo "步骤6：设置快捷命令..."

# 记录本机IP到配置文件，以后 start-all.sh 自动读取
echo "$DETECTED_IP" > "$SCRIPT_DIR/.local-ip"

echo ""
echo "=========================================="
echo "✅ 自动配置完成！"
echo "=========================================="
echo ""
echo "下一步：启动所有服务"
echo "  bash start-all.sh"
echo ""
echo "启动后访问："
echo "  管理后台:  http://localhost:5173"
echo "  后端API:   http://localhost:5000"
echo "  本机IP:    http://$DETECTED_IP:5173 （手机调试）"
echo ""