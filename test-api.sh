#!/bin/bash

# API测试脚本
echo "=== WO物业管理软件API测试 ==="
echo "测试时间: $(date)"
echo ""

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# 测试函数
test_endpoint() {
    local name=$1
    local url=$2
    local method=${3:-GET}
    local data=${4:-""}
    
    echo -n "测试 $name ($method $url)... "
    
    if [ "$method" = "POST" ] && [ -n "$data" ]; then
        response=$(curl -s -X "$method" -H "Content-Type: application/json" -d "$data" "$url" -w " HTTP_STATUS:%{http_code}")
    else
        response=$(curl -s -X "$method" "$url" -w " HTTP_STATUS:%{http_code}")
    fi
    
    http_status=$(echo "$response" | grep -o 'HTTP_STATUS:[0-9]*' | cut -d: -f2)
    response_body=$(echo "$response" | sed 's/ HTTP_STATUS:[0-9]*$//')
    
    if [ "$http_status" = "200" ] || [ "$http_status" = "201" ]; then
        echo -e "${GREEN}✓ 成功 (HTTP $http_status)${NC}"
        echo "  响应: $response_body" | head -c 100
        echo "..."
        return 0
    else
        echo -e "${RED}✗ 失败 (HTTP $http_status)${NC}"
        echo "  错误: $response_body"
        return 1
    fi
}

echo "1. 测试服务健康检查..."
echo "----------------------"
test_endpoint "认证服务健康检查" "http://localhost:5002/health"
test_endpoint "设备服务健康检查" "http://localhost:5003/health"

echo ""
echo "2. 测试认证API..."
echo "----------------"
# 使用管理员账户登录
admin_login='{"username":"admin","password":"Admin@123"}'
test_endpoint "管理员登录" "http://localhost:5002/api/auth/login" "POST" "$admin_login"

# 从响应中提取token（简化处理）
echo ""
echo "3. 测试工单API（需要认证）..."
echo "--------------------------"
echo "注意：需要有效的JWT令牌进行完整测试"
test_endpoint "获取工单列表" "http://localhost:5002/api/tickets"

echo ""
echo "4. 测试设备API..."
echo "----------------"
# 使用设备服务的登录
device_login='{"username":"admin","password":"Admin@123"}'
test_endpoint "设备服务登录" "http://localhost:5003/api/auth/login" "POST" "$device_login"
test_endpoint "获取设备列表" "http://localhost:5003/api/devices"
test_endpoint "获取设备分类" "http://localhost:5003/api/device-categories"
test_endpoint "获取位置列表" "http://localhost:5003/api/locations"

echo ""
echo "5. 服务状态总结..."
echo "-----------------"
echo "认证与工单服务:"
curl -s http://localhost:5002/health | jq '. | {service, version, status, timestamp}'

echo ""
echo "设备管理服务:"
curl -s http://localhost:5003/health | jq '. | {service, version, status, timestamp}'

echo ""
echo "=== 测试完成 ==="
echo ""
echo "💡 演示账户:"
echo "  • 管理员: admin / Admin@123"
echo "  • 技术人员: tech / Tech@123"
echo "  • 普通用户: user / User@123"
echo ""
echo "🌐 服务地址:"
echo "  • 认证与工单服务: http://localhost:5002"
echo "  • 设备管理服务: http://localhost:5003"
echo "  • 前端测试页面: file:///Users/mac/Projects/WO-Property-Management/src/admin-portal/test.html"
echo ""
echo "📁 项目文档: ~/Projects/WO-Property-Management/SERVICES_STATUS.md"
