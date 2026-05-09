#!/bin/bash
# WO Property Management System - 首次启动脚本
# 负责：创建网络、启动 MySQL 初始化卷、启动所有服务

set -e

PROJECT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
cd "$PROJECT_DIR"

COMPOSE_FILE="docker-compose.yml"

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

log_info()  { echo -e "${GREEN}[INFO]${NC} $1"; }
log_warn()  { echo -e "${YELLOW}[WARN]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

usage() {
    cat <<EOF
用法: $0 [--build] [--skip-db]

选项:
  --build    构建所有镜像（首次运行或代码更新后使用）
  --skip-db  跳过 MySQL 健康检查等待（已初始化数据库时使用）

示例:
  $0           # 快速启动（假设镜像已构建）
  $0 --build   # 首次构建并启动
EOF
}

BUILD_FLAG=false
SKIP_DB=false

while [[ $# -gt 0 ]]; do
    case $1 in
        --build)   BUILD_FLAG=true; shift ;;
        --skip-db) SKIP_DB=true;   shift ;;
        --help|-h) usage; exit 0 ;;
        *)         shift ;;
    esac
done

log_info "========== WO Property 容器化部署 =========="

# 1. 清理旧容器（如果存在）
log_info "检查并清理旧容器..."
docker compose -f "$COMPOSE_FILE" down --remove-orphans 2>/dev/null || true

# 2. 构建镜像
if $BUILD_FLAG; then
    log_warn "首次构建，请耐心等待（首次下载 .NET SDK 镜像较慢）..."
    docker compose -f "$COMPOSE_FILE" build --no-cache
else
    log_info "跳过构建（使用已有镜像）。如需构建请加 --build"
fi

# 3. 创建网络（如果不存在）
log_info "创建 Docker 网络..."
docker network create wo-network 2>/dev/null || log_info "网络已存在，跳过"

# 4. 启动数据库（先行启动，等健康）
log_info "启动 MySQL..."
docker compose -f "$COMPOSE_FILE" up -d mysql

if ! $SKIP_DB; then
    log_info "等待 MySQL 健康就绪（最多 60s）..."
    timeout=60
    while [ $timeout -gt 0 ]; do
        STATUS=$(docker compose -f "$COMPOSE_FILE" ps mysql 2>/dev/null | grep -o "health\|starting" | head -1)
        if [[ "$STATUS" == "health" ]]; then
            log_info "MySQL 已就绪"
            break
        fi
        echo -n "."
        sleep 2
        timeout=$((timeout - 2))
    done
    echo ""
    if [ $timeout -le 0 ]; then
        log_error "MySQL 健康检查超时！请检查日志: docker compose logs mysql"
        exit 1
    fi
else
    log_warn "跳过 MySQL 健康等待（--skip-db）"
fi

# 5. 启动所有服务
log_info "启动所有服务..."
docker compose -f "$COMPOSE_FILE" up -d

# 6. 等待所有服务健康
log_info "等待所有服务健康就绪..."
sleep 5

UNHEALTHY=$(docker compose -f "$COMPOSE_FILE" ps 2>/dev/null | grep -v "health\|Up\|nginx\|mysql" | grep -v "^$" | wc -l)
if [ "$UNHEALTHY" -gt 0 ]; then
    log_warn "部分服务可能尚未完全就绪，请运行以下命令检查:"
    echo "  docker compose -f $COMPOSE_FILE ps"
    echo "  docker compose -f $COMPOSE_FILE logs [服务名]"
else
    log_info "所有服务已启动！"
fi

echo ""
echo "========== 部署完成 =========="
echo "  访问地址:   http://localhost"
echo "  API Gateway: http://localhost:5000"
echo ""
echo "  查看状态:   docker compose -f $COMPOSE_FILE ps"
echo "  查看日志:   docker compose -f $COMPOSE_FILE logs -f"
echo "  停止服务:   ./scripts/docker-down.sh"
