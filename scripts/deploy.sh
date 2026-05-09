#!/bin/bash
# WO Property Management System - Docker 部署脚本
# 用法: ./scripts/deploy.sh [服务名] [--build]
#   无参数    → 重启所有服务
#   [服务名]  → 仅重启指定服务 (gateway|auth-service|masterdata-service|...)
#   --build   → 重新构建镜像后再启动

set -e

PROJECT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
cd "$PROJECT_DIR"

COMPOSE_FILE="docker-compose.yml"

# 颜色
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

log_info()  { echo -e "${GREEN}[INFO]${NC} $1"; }
log_warn()  { echo -e "${YELLOW}[WARN]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

# 帮助
usage() {
    cat <<EOF
用法: $0 [服务名] [--build]

示例:
  $0                  # 重启所有服务
  $0 gateway          # 仅重启 gateway
  $0 --build          # 重新构建并重启所有服务
  $0 auth-service --build  # 重新构建并重启 auth-service
EOF
}

# 解析参数
BUILD_FLAG=false
SERVICE_NAME=""

while [[ $# -gt 0 ]]; do
    case $1 in
        --build)
            BUILD_FLAG=true
            shift
            ;;
        --help|-h)
            usage
            exit 0
            ;;
        *)
            SERVICE_NAME="$1"
            shift
            ;;
    esac
done

# 如果没有指定服务名，默认全部
ALL_SERVICES=false
if [[ -z "$SERVICE_NAME" ]]; then
    ALL_SERVICES=true
fi

log_info "开始部署..."

# 构建函数
do_build() {
    local svc="$1"
    log_info "正在构建镜像: $svc"
    docker compose -f "$COMPOSE_FILE" build "$svc" --no-cache
}

# 启动函数
do_start() {
    local svc="$1"
    log_info "正在启动服务: $svc"
    docker compose -f "$COMPOSE_FILE" up -d "$svc"
}

# 重启函数
do_restart() {
    local svc="$1"
    log_info "正在重启服务: $svc"
    docker compose -f "$COMPOSE_FILE" restart "$svc"
}

if $ALL_SERVICES; then
    if $BUILD_FLAG; then
        log_warn "全量构建中（无缓存），这可能需要几分钟..."
        docker compose -f "$COMPOSE_FILE" build --no-cache
        docker compose -f "$COMPOSE_FILE" up -d
    else
        log_info "重启所有服务..."
        docker compose -f "$COMPOSE_FILE" restart
    fi
else
    if $BUILD_FLAG; then
        do_build "$SERVICE_NAME"
    fi
    do_restart "$SERVICE_NAME"
fi

log_info "部署完成！"
echo ""
echo "  服务状态: docker compose -f $COMPOSE_FILE ps"
echo "  日志:     docker compose -f $COMPOSE_FILE logs -f [服务名]"
echo "  访问:     http://localhost"
