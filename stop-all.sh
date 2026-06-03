#!/bin/bash
# WO Property Management - 停止所有服务
# 用法: bash stop-all.sh

echo "正在停止 WO 物业管理服务..."

PORTS="5000 5106 5102 5241 5018 5019 5173"

for port in $PORTS; do
  pid=$(lsof -i :$port -s TCP:LISTEN -t 2>/dev/null)
  if [ -n "$pid" ]; then
    echo "  停止端口 $port (PID: $pid)..."
    kill $pid 2>/dev/null || true
  fi
done

# 停止残留的 dotnet 进程（ WO.Property 相关）
pkill -f "WO.Property\." 2>/dev/null || true

echo ""
echo "所有服务已停止"