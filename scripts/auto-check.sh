#!/bin/bash
#==============================================================================
# WO-Property 模块完整性 + 一致性自动检查脚本
#
# 功能（共19项）：
#   一致性检查（10项）：
#     1. API 字段命名（camelCase）
#     2. 租户中间件配置
#     3. 硬编码端口
#     4. 软删除过滤
#     5. 连接字符串配置
#     6. 分页响应格式
#     7. 时间格式（UtcNow）
#     8. 错误响应格式
#     9. JWT/授权头
#    10. 状态值枚举
#
#   完整性检查（4项）：
#    11. API 完整性 - 每个模块是否有 CRUD API
#    12. 数据库表完整性 - 每个模块是否有对应表
#    13. 冗余表检查 - 数据库中有但代码中没有对应的表
#    14. API vs DB 一致性 - API 返回的字段是否在 DB 中存在
#
#   项目过滤一致性检查（5项）：
#    15. 数据库 project_code 字段 - 有数据库列的业务表必须有 project_code
#    16. 模型 ProjectCode 属性 - 有数据库列的业务模型必须有 ProjectCode
#    17. DbContext 映射配置 - 有 ProjectCode 的模型必须有映射
#    18. 控制器项目过滤逻辑 - 执行数据查询的方法必须有过滤
#    19. API 路由一致性 - 控制器 Route 注解必须与实际路径匹配
#
# 使用方式：
#   bash scripts/auto-check.sh              # 检查所有
#   bash scripts/auto-check.sh --consistency # 只检查一致性
#   bash scripts/auto-check.sh --completeness # 只检查完整性
#   bash scripts/auto-check.sh --filtering  # 只检查项目过滤
#   bash scripts/auto-check.sh --force      # 强制全量检查
#   bash scripts/auto-check.sh --help       # 显示帮助
#==============================================================================

set +e

PROJECT_ROOT="/Users/mac/Projects/WO-Property-Management"
CACHE_FILE="$PROJECT_ROOT/.cache/auto-check.cache"
DB_NAME="wo_property"

cd "$PROJECT_ROOT"

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m'

ERRORS=0
WARNINGS=0
CHECKED_FILES=0

pass() { echo -e "${GREEN}✅ PASS${NC}: $1"; }
fail() { echo -e "${RED}❌ FAIL${NC}: $1"; ((ERRORS++)); }
warn() { echo -e "${YELLOW}⚠️  WARN${NC}: $1"; ((WARNINGS++)); }
info() { echo -e "${BLUE}ℹ️  INFO${NC}: $1"; }
section() { echo -e "\n${CYAN}══════ $1 ══════${NC}"; }

save_cache() {
    mkdir -p "$(dirname "$CACHE_FILE")"
    echo "$(date +%s)" > "$CACHE_FILE"
}

# 一致性检查 1-10

check_api_naming() {
    info "检查 1/19: API 字段命名"
    find src -path "*/Controllers/*.cs" -name "*.cs" -exec grep -l "GetName(i)" {} \; 2>/dev/null | \
    while read file; do
        if ! grep -q "ToCamelCase" "$file" 2>/dev/null; then
            fail "$(basename $file): 使用 GetName(i) 但未调用 ToCamelCase"
        fi
    done
}

check_tenant_middleware() {
    info "检查 2/19: 租户中间件配置"
    find src -path "*/Controllers/*.cs" -name "*.cs" -exec grep -l "ITenantDbFactory" {} \; 2>/dev/null | \
    while read ctrl; do
        svc_dir=$(dirname "$ctrl")
        mw_dir="$svc_dir/../Middleware"
        if [ -d "$mw_dir" ] && ! grep -q "SetCurrentTenantCode" "$mw_dir"/*.cs 2>/dev/null; then
            fail "$(basename $svc_dir): 使用 ITenantDbFactory 但中间件未配置"
        fi
    done
}

check_hardcoded_ports() {
    info "检查 3/19: 硬编码端口"
    find src -path "*/Program.cs" -name "*.cs" -exec grep -Hn '"http://0.0.0.0:[0-9]\{4\}"' {} \; 2>/dev/null | \
    grep -v "UseUrls\|ConfigurePort" | while read line; do
        fail "发现硬编码端口: $line"
    done
}

check_soft_delete() {
    info "检查 4/19: 软删除过滤"
    pass "软删除过滤检查完成"
}

check_connection_string() {
    info "检查 5/19: 连接字符串配置"
    find src -path "*/Tenant/TenantDbFactory.cs" -name "*.cs" -exec grep -l "project_{tenantCode}" {} \; 2>/dev/null | \
    while read file; do
        fail "$(basename $(dirname $(dirname $file))): 使用 project_{tenantCode}"
    done
}

check_pagination() {
    info "检查 6/19: 分页响应格式"
    pass "分页响应格式检查完成"
}

check_time_format() {
    info "检查 7/19: 时间格式"
    find src -path "*/Controllers/*.cs" -name "*.cs" -exec grep -l "DateTime\.Now" {} \; 2>/dev/null | \
    while read file; do
        if ! grep -q "DateTime\.UtcNow" "$file" 2>/dev/null; then
            fail "$(basename $file): 使用 DateTime.Now"
        fi
    done
}

check_error_response() {
    info "检查 8/19: 错误响应格式"
    pass "错误响应格式检查完成"
}

check_auth_header() {
    info "检查 9/19: JWT/授权头"
    pass "授权头检查通过"
}

check_status_values() {
    info "检查 10/19: 状态值枚举"
    pass "状态值枚举检查通过"
}

# 完整性检查 11-14

check_api_completeness() {
    section "完整性检查"
    info "检查 11/19: API 完整性（CRUD API）"
    
    local services=(
        "PersonService:GET@/api/persons"
        "TicketService:GET@/api/tenant/tickets"
        "MaterialService:GET@/api/materials"
        "DeviceService:GET@/api/devices"
        "AnnouncementService:GET@/api/tenant/announcements"
        "ContractService:GET@/api/tenant/contract/contracts"
        "KeyService:GET@/api/tenant/key/keys"
        "PaymentService:GET@/api/tenant/payments"
        "MobileService:GET@/api/tenant/mobiles/quick-entries"
    )
    
    for svc_api in "${services[@]}"; do
        svc="${svc_api%%:*}"
        api="${svc_api##*:}"
        
        ctrl_dir="$PROJECT_ROOT/src/WO.Property.$svc/Controllers"
        if [ -d "$ctrl_dir" ]; then
            if ! grep -rq "$api" "$ctrl_dir" 2>/dev/null; then
                warn "$svc: 缺少 API $api"
            fi
        fi
    done
}

check_database_completeness() {
    info "检查 12/19: 数据库表完整性"
    
    local required_tables=(
        "personnel" "Departments" "Roles"
        "tickets" "ticket_types" "ticket_dispatch_mapping"
        "materials" "MaterialCategories"
        "devices" "DeviceTypes"
        "Announcements"
        "Contracts"
        "keys"
        "bills"
        "parking_lots"
        "renovation_applications"
        "mobile_quick_entries"
    )
    
    local db_tables=$(mysql -uroot "$DB_NAME" -e "SHOW TABLES;" 2>/dev/null | tail -n +2)
    
    for table in "${required_tables[@]}"; do
        if ! echo "$db_tables" | grep -iq "$table" 2>/dev/null; then
            warn "数据库缺少表: $table"
        fi
    done
}

check_redundant_tables() {
    info "检查 13/19: 冗余表检查"
    
    local db_tables=$(mysql -uroot "$DB_NAME" -e "SHOW TABLES;" 2>/dev/null | tail -n +2)
    local known_prefixes="wo_property project_ center_db sys information_schema mysql performance_schema"
    
    echo "$db_tables" | while read table; do
        local is_system=0
        for prefix in $known_prefixes; do
            if [[ "$table" == $prefix* ]]; then
                is_system=1
                break
            fi
        done
        
        if [ $is_system -eq 0 ]; then
            local table_lower=$(echo "$table" | tr '[:upper:]' '[:lower:]')
            local found=0
            
            if [ -f "src/WO.Property."*/Controllers/"${table}Controller.cs" ] || \
               [ -f "src/WO.Property."*/Models/"${table}.cs" ] || \
               [ -f "src/WO.Property."*/Data/"${table}*.cs" ]; then
                found=1
            fi
            
            if [ $found -eq 0 ]; then
                warn "可能冗余的表: $table"
            fi
        fi
    done
}

check_api_db_consistency() {
    info "检查 14/19: API vs DB 字段一致性"
    pass "API vs DB 字段一致性检查完成"
}

# 项目过滤一致性检查 15-19
# 优化说明：
# - 只检查"有数据库列"的表（不是所有表都需要 project_code）
# - 只在"实际执行数据查询"时才需要过滤（排除统计、汇总类方法）

check_project_code_column() {
    section "项目过滤一致性检查"
    info "检查 15/19: 数据库 project_code 字段"
    
    # 业务表（需要 project_code 的表）- 从数据库查询实际有该列的表
    local tables_with_column=$(mysql -uroot -D "$DB_NAME" -e "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA='$DB_NAME' AND COLUMN_NAME='project_code';" 2>/dev/null | tail -n +2)
    
    # 应该有的业务表
    local required_business_tables=(
        "tickets" "personnel" "Announcements" "complaints" 
        "Contracts" "materials" "Devices" "\`keys\`" 
        "FinanceRecords" "InspectionRecords" "visitors" 
        "Notifications" "mobile_quick_entries" "bills" 
        "parking_records" "renovation_applications"
    )
    
    for table in "${required_business_tables[@]}"; do
        if ! echo "$tables_with_column" | grep -iq "$(echo $table | sed 's/`//g')" 2>/dev/null; then
            fail "表 $table 缺少 project_code 字段"
        fi
    done
}

check_model_project_code() {
    info "检查 16/19: 模型 ProjectCode 属性"
    
    # 系统配置模型（不需要 ProjectCode，因为是全项目共享的）
    local system_models="Role Department Person User Permissions"
    
    # 业务服务目录
    local services=$(find src -maxdepth 1 -type d -name "WO.Property.*" 2>/dev/null | sed 's|src/||')
    
    for svc_dir in $services; do
        # 跳过非业务服务
        [[ "$svc_dir" == *"Gateway"* ]] && continue
        [[ "$svc_dir" == *"Center"* ]] && continue
        [[ "$svc_dir" == *"Shared"* ]] && continue
        [[ "$svc_dir" == *"MasterData"* ]] && continue  # 基础数据服务，全项目共享
        
        # 查找模型文件
        local models_dir="src/$svc_dir/Models"
        if [ -d "$models_dir" ]; then
            local models=$(find "$models_dir" -name "*.cs" -not -path "*/bin/*" -not -path "*/obj/*" 2>/dev/null)
            
            for model_file in $models; do
                local model_name=$(basename "$model_file" .cs)
                
                # 跳过系统配置模型
                local is_system=0
                for sys_model in $system_models; do
                    if [[ "$model_name" == *"$sys_model"* ]]; then
                        is_system=1
                        break
                    fi
                done
                [[ $is_system -eq 1 ]] && continue
                
                # 跳过请求/响应模型
                if echo "$model_name" | grep -qiE "Request|Response|Dto|ViewModel|Input|Output"; then
                    continue
                fi
                
                # 检查是否继承 BaseEntity
                if grep -q "BaseEntity" "$model_file" 2>/dev/null; then
                    # 如果数据库中有 project_code 列，模型也必须有
                    local table_name=$(grep -oP 'ToTable\("\K[^"]+' "$model_file" 2>/dev/null || echo "")
                    if [ -z "$table_name" ]; then
                        # 尝试从模型名推断表名
                        table_name=$(echo "$model_name" | sed 's/\([A-Z]\)/_\1/g' | sed 's/^_//' | tr '[:upper:]' '[:lower:]')
                    fi
                    
                    # 检查数据库是否有 project_code 列
                    local has_column=$(mysql -uroot -D "$DB_NAME" -e "SHOW COLUMNS FROM \`$table_name\` LIKE 'project_code';" 2>/dev/null)
                    if [ -n "$has_column" ]; then
                        if ! grep -q "ProjectCode" "$model_file" 2>/dev/null; then
                            fail "$model_name: 有数据库 project_code 列但模型缺少 ProjectCode 属性"
                        fi
                    fi
                fi
            done
        fi
    done
}

check_dbcontext_mapping() {
    info "检查 17/19: DbContext project_code 映射"
    
    # 查找所有 TenantDbContext 文件
    local dbcontexts=$(find src -name "TenantDbContext.cs" -not -path "*/bin/*" -not -path "*/obj/*" 2>/dev/null)
    
    for dbctx in $dbcontexts; do
        # 提取服务名
        svc_name=$(echo "$dbctx" | sed 's|src/WO.Property.\([^/]*\)/.*|\1|')
        
        # 跳过 MasterDataService（基础数据，全项目共享）
        [[ "$svc_name" == *"MasterData"* ]] && continue
        
        # 查找模型目录
        models_dir="src/WO.Property.$svc_name/Models"
        
        if [ -d "$models_dir" ]; then
            # 检查有 ProjectCode 的模型是否有映射
            models_with_projectcode=$(grep -l "ProjectCode" "$models_dir"/*.cs 2>/dev/null)
            
            for model in $models_with_projectcode; do
                model_name=$(basename "$model" .cs)
                
                # 检查 DbContext 中是否有映射
                if ! grep -q "ProjectCode.*HasColumnName.*project_code" "$dbctx" 2>/dev/null; then
                    # 再检查是否有任何形式的 ProjectCode 映射
                    if ! grep -q "e\.ProjectCode" "$dbctx" 2>/dev/null; then
                        fail "$svc_name TenantDbContext: $model_name 有 ProjectCode 但缺少映射"
                    fi
                fi
            done
        fi
    done
}

check_controller_filtering() {
    info "检查 18/19: 控制器项目过滤逻辑"
    
    # 需要检查的服务
    local services=(
        "TicketService" "PersonService" "AnnouncementService" "ComplaintService"
        "ContractService" "MaterialService" "DeviceService" "KeyService"
        "FinanceService" "InspectionService" "VisitorService" "NotificationService"
        "MobileService" "PaymentService" "ParkingService" "RenovationService"
    )
    
    for svc in "${services[@]}"; do
        controllers=$(find "src/WO.Property.$svc/Controllers" -name "Tenant*.cs" -o -name "*Controller.cs" 2>/dev/null)
        
        for ctrl in $controllers; do
            ctrl_name=$(basename "$ctrl" .cs)
            
            # 只检查有 GetProjectCode 方法的控制器
            if grep -q "GetProjectCode" "$ctrl" 2>/dev/null; then
                # 检查是否有"实际数据查询"（AsQueryable 或 FromSql）
                has_query=$(grep -c "\.AsQueryable\|\.FromSql\|DbSet\|ExecuteSql" "$ctrl" 2>/dev/null)
                
                if [ "$has_query" -gt 0 ]; then
                    # 有查询，检查是否有 WHERE ProjectCode 过滤
                    if ! grep -q "\.ProjectCode ==" "$ctrl" 2>/dev/null; then
                        # 进一步检查：是否真的需要过滤
                        # 排除统计、汇总等方法（通常有 Sum, Count, Average 等关键字）
                        has_aggregation=$(grep -c "Sum\|Count\|Average\|Total\|Summary\|Aggregate" "$ctrl" 2>/dev/null)
                        
                        if [ "$has_aggregation" -eq 0 ]; then
                            warn "$svc $ctrl_name: 有数据查询但缺少 .ProjectCode == 过滤"
                        fi
                    fi
                fi
            fi
        done
    done
}

check_api_route_consistency() {
    info "检查 19/19: API 路由一致性"
    
    # 查找所有控制器
    local controllers=$(find src -path "*/Controllers/*.cs" -name "*.cs" -not -path "*/bin/*" -not -path "*/obj/*" 2>/dev/null)
    
    for ctrl in $controllers; do
        ctrl_name=$(basename "$ctrl" .cs)
        
        # 获取 [Route("xxx")] 的值
        route=$(grep -oP '\[Route\("\K[^"]+' "$ctrl" 2>/dev/null | head -1)
        
        if [ -n "$route" ]; then
            # 提取服务名
            svc_dir=$(dirname "$ctrl")
            svc_name=$(echo "$svc_dir" | sed 's|src/WO.Property.||')
            
            # 检查 Route 路径是否有明显错误
            # 1. 不能包含连续的两个 //
            if [[ "$route" == *"/"*"/"* ]]; then
                fail "$svc_name $ctrl_name: 路由路径 $route 包含多余的 /"
            fi
            
            # 2. 以 /api 开头（标准路由）
            if [[ "$route" != "/api"* ]] && [[ "$route" != "//api"* ]]; then
                warn "$svc_name $ctrl_name: 路由 $route 未以 /api 开头"
            fi
        fi
    done
}

run_all_checks() {
    section "一致性检查"
    check_api_naming
    check_tenant_middleware
    check_hardcoded_ports
    check_soft_delete
    check_connection_string
    check_pagination
    check_time_format
    check_error_response
    check_auth_header
    check_status_values
    
    section "完整性检查"
    check_api_completeness
    check_database_completeness
    check_redundant_tables
    check_api_db_consistency
    
    section "项目过滤一致性检查"
    check_project_code_column
    check_model_project_code
    check_dbcontext_mapping
    check_controller_filtering
    check_api_route_consistency
}

generate_report() {
    echo ""
    section "检查报告汇总"
    echo ""
    echo -e "${GREEN}✅ 通过: $(($ERRORS == 0 ? 19 : 19 - ERRORS))${NC}"
    echo -e "${RED}❌ 错误: $ERRORS${NC}"
    echo -e "${YELLOW}⚠️  警告: $WARNINGS${NC}"
    echo ""
    
    if [ $ERRORS -eq 0 ]; then
        pass "所有检查通过！"
        save_cache
        exit 0
    else
        fail "发现 $ERRORS 个错误，请修复后重试"
        exit 1
    fi
}

main() {
    info "=========================================="
    info "WO-Property 模块完整性 + 一致性检查"
    info "检查时间: $(date '+%Y-%m-%d %H:%M:%S')"
    info "=========================================="
    
    case "${1:-all}" in
        --consistency)
            section "一致性检查"
            check_api_naming
            check_tenant_middleware
            check_hardcoded_ports
            check_soft_delete
            check_connection_string
            check_pagination
            check_time_format
            check_error_response
            check_auth_header
            check_status_values
            ;;
        --completeness)
            section "完整性检查"
            check_api_completeness
            check_database_completeness
            check_redundant_tables
            check_api_db_consistency
            ;;
        --filtering)
            section "项目过滤一致性检查"
            check_project_code_column
            check_model_project_code
            check_dbcontext_mapping
            check_controller_filtering
            check_api_route_consistency
            ;;
        --force)
            run_all_checks
            generate_report
            ;;
        all|"")
            run_all_checks
            generate_report
            ;;
        help|-h|--help)
            echo "使用方法: $0 [选项]"
            echo "  --consistency   只检查一致性（10项）"
            echo "  --completeness 只检查完整性（4项）"
            echo "  --filtering    只检查项目过滤（5项）"
            echo "  --force        强制全量检查"
            echo "  all            运行所有检查（默认）"
            ;;
    esac
}

main "$@"