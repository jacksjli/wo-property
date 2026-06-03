#!/bin/bash
# WO Property Management - 服务诊断脚本
# 兼容 macOS bash 3.2+
# 用法: bash scripts/service-diagnostics.sh

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
PORTS_FILE="$PROJECT_DIR/config/ports.json"

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}  WO Property - 服务诊断工具${NC}"
echo -e "${BLUE}========================================${NC}"
echo ""

# 检查 ports.json 是否存在
if [ ! -f "$PORTS_FILE" ]; then
    echo -e "${RED}错误: config/ports.json 不存在${NC}"
    exit 1
fi

echo -e "${YELLOW}读取端口配置: $PORTS_FILE${NC}"
echo ""

# 临时文件存储结果
TMPFILE="/tmp/wo_diag_$$.txt"
rm -f "$TMPFILE"

# 解析 ports.json 并测试每个服务
echo -e "${YELLOW}正在检测所有服务健康状态...${NC}"
echo ""

current_name=""
while IFS= read -r line; do
    # 匹配 "name": "ServiceName"
    if [[ "$line" =~ \"name\"[[:space:]]*:[[:space:]]*\"([^\"]+)\" ]]; then
        current_name="${BASH_REMATCH[1]}"
    fi
    # 匹配 "port": 5000
    if [[ "$line" =~ \"port\"[[:space:]]*:[[:space:]]*([0-9]+) ]]; then
        current_port="${BASH_REMATCH[1]}"
        if [ -n "$current_name" ] && [ -n "$current_port" ]; then
            # 测试健康端点
            response=$(curl -s -o /dev/null -w "%{http_code}" --max-time 3 "http://localhost:$current_port/health" 2>/dev/null || echo "000")
            
            if [ "$response" = "200" ]; then
                status="healthy"
            elif [ "$response" = "404" ]; then
                # 没有 /health 端点，尝试根路径检测
                alt_check=$(curl -s --max-time 3 "http://localhost:$current_port/" 2>/dev/null | head -c 10)
                if [ -n "$alt_check" ]; then
                    status="no_health_endpoint"
                else
                    status="no_response"
                fi
            else
                status="unhealthy"
            fi
            
            echo "$current_name|$current_port|$status" >> "$TMPFILE"
            unset current_name current_port
        fi
    fi
done < "$PORTS_FILE"

# 检测前端 (5173)
node_check=$(lsof -i :5173 2>/dev/null | grep LISTEN | wc -l | tr -d ' ')
if [ "$node_check" -gt 0 ]; then
    echo "admin-portal|5173|healthy" >> "$TMPFILE"
else
    echo "admin-portal|5173|no_response" >> "$TMPFILE"
fi

# 打印结果
printf "%-25s %-8s %s\n" "服务名称" "端口" "状态"
printf "%-25s %-8s %s\n" "--------" "----" "----"

total=0
healthy=0
unhealthy=0

while IFS='|' read -r name port status; do
    ((total++))
    
    case $status in
        healthy)
            icon="${GREEN}✅${NC}"
            ((healthy++))
            ;;
        no_health_endpoint)
            icon="${YELLOW}⚠️ 无/health${NC}"
            ((healthy++))
            ;;
        no_response|unhealthy)
            icon="${RED}❌${NC}"
            ((unhealthy++))
            ;;
        *)
            icon="${RED}❌${NC}"
            ((unhealthy++))
            ;;
    esac
    
    printf "%-25s %-8s %b\n" "$name" "$port" "$icon"
done < "$TMPFILE"

# 清理
rm -f "$TMPFILE"

echo ""
echo -e "${BLUE}========================================${NC}"
echo -e "总计: $total 个服务 | ${GREEN}健康: $healthy${NC} | ${RED}异常: $unhealthy${NC}"
echo -e "${BLUE}========================================${NC}"

# 生成修复建议
if [ "$unhealthy" -gt 0 ]; then
    echo ""
    echo -e "${YELLOW}【修复建议】${NC}"
    echo ""
    echo "  # 一键启动所有服务:"
    echo "  bash $PROJECT_DIR/scripts/start-services.sh"
fi

echo ""
echo "诊断完成: $(date '+%Y-%m-%d %H:%M:%S')"