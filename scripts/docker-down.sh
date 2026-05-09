#!/bin/bash
# WO Property Management System - 停止所有容器脚本

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

usage() {
    cat <<EOF
用法: $0 [--clean] [--volumes]

选项:
  --clean   停止容器并清理镜像（节省磁盘空间）
  --volumes 停止容器并删除数据卷（慎用！会丢失数据库数据！）

示例:
  $0             # 停止所有容器
  $0 --clean     # 停止并清理镜像
  $0 --volumes   # 停止并删除数据卷（不可恢复！）
EOF
}

CLEAN_FLAG=false
VOLUMES_FLAG=false

while [[ $# -gt 0 ]]; do
    case $1 in
        --clean)   CLEAN_FLAG=true; shift ;;
        --volumes) VOLUMES_FLAG=true; shift ;;
        --help|-h) usage; exit 0 ;;
        *)         shift ;;
    esac
done

log_info "========== 停止 WO Property 容器 =========="

if $VOLUMES_FLAG; then
    log_warn "删除数据卷！数据库数据将永久丢失！"
    read -p "确认删除? (y/N): " confirm
    if [[ "$confirm" != "y" && "$confirm" != "Y" ]]; then
        log_info "取消操作"
        exit 0
    fi
    log_info "停止容器并删除数据卷..."
    docker compose -f "$COMPOSE_FILE" down -v --remove-orphans
    log_info "数据卷已删除"
elif $CLEAN_FLAG; then
    log_info "停止容器并清理镜像..."
    docker compose -f "$COMPOSE_FILE" down --remove-orphans
    log_info "清理未使用的镜像..."
    docker image prune -f
else
    log_info "停止所有容器（保留数据卷）..."
    docker compose -f "$COMPOSE_FILE" down --remove-orphans
fi

log_info "========== 完成 =========="
echo ""
echo "  重新启动: ./scripts/docker-up.sh"
echo "  重新部署: ./scripts/deploy.sh"
