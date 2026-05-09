#!/bin/bash
# ============================================================
# WO-Property CD 部署脚本（远程服务器执行）
# 由 GitHub Actions ssh-action 调用
# 用法: ./scripts/cicd/deploy.sh [env] [version]
# ============================================================

set -e

ENV="${1:-prod}"
VERSION="${2:-latest}"
PROJECT_DIR="/opt/wo-property"
REGISTRY="${REGISTRY:-ghcr.io}"
IMAGE_PREFIX="${IMAGE_PREFIX:-}"

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

log_info()  { echo -e "${GREEN}[INFO]${NC} $1"; }
log_warn()  { echo -e "${YELLOW}[WARN]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

log_info "=========================================="
log_info "  WO-Property CD 部署脚本"
log_info "=========================================="
log_info "  环境:   $ENV"
log_info "  版本:   $VERSION"
log_info "  目录:   $PROJECT_DIR"
log_info "=========================================="

cd "$PROJECT_DIR"

# 生成 docker-compose override 文件
generate_override() {
    local env_file="$PROJECT_DIR/docker-compose.${ENV}.yml"
    cat > "$env_file" << 'YML'
version: '3.8'
services:
YML

    local services=("gateway" "auth-service" "masterdata-service" "person-service" "ticket-service" "frontend")
    for svc in "${services[@]}"; do
        echo "  ${svc}:" >> "$env_file"
        echo "    image: ${REGISTRY}/${IMAGE_PREFIX}/${svc}:${VERSION}" >> "$env_file"
        echo "    pull_policy: always" >> "$env_file"
    done

    log_info "✅ Override 文件已生成: $env_file"
}

# 备份当前配置
backup_config() {
    if [[ -f "$PROJECT_DIR/docker-compose.yml" ]]; then
        BACKUP_NAME="docker-compose.yml.backup.$(date +%Y%m%d%H%M%S)"
        cp "$PROJECT_DIR/docker-compose.yml" "$PROJECT_DIR/$BACKUP_NAME"
        log_info "📦 配置已备份: $BACKUP_NAME"
    fi
}

# 拉取新镜像
pull_images() {
    log_info "📦 开始拉取镜像..."
    local services=("gateway" "auth-service" "masterdata-service" "person-service" "ticket-service" "frontend")
    for svc in "${services[@]}"; do
        local img="${REGISTRY}/${IMAGE_PREFIX}/${svc}:${VERSION}"
        log_info "  → 拉取 $img"
        docker pull "$img" || log_warn "  ⚠️ 拉取失败: $img"
    done
}

# 重启服务
restart_services() {
    log_info "🚀 重启服务（不重新构建）..."
    docker compose -f docker-compose.yml -f "docker-compose.${ENV}.yml" up -d --no-build
    log_info "✅ 服务启动完成"
}

# 健康检查
health_check() {
    log_info "🩺 执行健康检查..."
    local max_retries=30
    local retry_interval=10
    local host="${PROD_SERVER_HOST:-localhost}"
    local port="${PROD_SERVER_PORT:-80}"

    for i in $(seq 1 $max_retries); do
        echo "  [$i/$max_retries] 检查中..."

        # 检查前端
        http_code=$(curl -s -o /dev/null -w "%{http_code}" "http://${host}:${port}/" 2>/dev/null || echo "000")
        if [[ "$http_code" == "200" ]]; then
            log_info "  ✅ 前端正常 (HTTP $http_code)"
        else
            log_warn "  ⏳ 前端异常 (HTTP $http_code)，重试中..."
            sleep $retry_interval
            continue
        fi

        # 检查网关
        gateway_code=$(curl -s -o /dev/null -w "%{http_code}" "http://${host}:${port}/api/health" 2>/dev/null || echo "000")
        if [[ "$gateway_code" == "200" ]]; then
            log_info "  ✅ 网关正常 (HTTP $gateway_code)"
        else
            log_warn "  ⏳ 网关异常 (HTTP $gateway_code)，重试中..."
            sleep $retry_interval
            continue
        fi

        log_info "🎉 所有健康检查通过！"
        return 0
    done

    log_error "❌ 健康检查超时"
    return 1
}

# 回滚
rollback() {
    log_warn "⚠️ 开始回滚..."
    local backup=$(ls -t "$PROJECT_DIR"/docker-compose.yml.backup.* 2>/dev/null | head -1)
    if [[ -n "$backup" ]]; then
        log_info "📦 从 $backup 恢复..."
        cp "$backup" "$PROJECT_DIR/docker-compose.yml"
        docker compose -f docker-compose.yml up -d
        log_info "✅ 回滚完成"
    else
        log_error "⚠️ 没有找到备份，手动处理"
    fi
}

# ── 主流程 ─────────────────────────────────────────────────
generate_override
backup_config
pull_images
restart_services

if health_check; then
    log_info "=========================================="
    log_info "  🎉 部署成功！版本: $VERSION"
    log_info "=========================================="
else
    rollback
    log_error "=========================================="
    log_error "  ❌ 部署失败，已触发回滚"
    log_error "=========================================="
    exit 1
fi
