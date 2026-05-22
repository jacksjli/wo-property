#!/bin/bash
# Phase 9: 批量改造业务服务
# 为每个服务添加 X-Project 路由支持

set -e

PROJECT_ROOT="/Users/mac/Projects/WO-Property-Management"
SERVICES=(
    "DispatchService:5003"
    "ContractService:5014"
    "ExpressService:5016"
    "DeliveryService:5017"
    "MaterialService:5072"
    "KeyService:5088"
    "DeviceService:5093"
    "NotificationService:5129"
    "VisitorService:5178"
    "StatisticsService:5241"
    "FinanceService:5281"
    "InspectionService:5296"
)

echo "=== Phase 9: 批量改造业务服务 ==="
echo "需要改造的服务数量: ${#SERVICES[@]}"
echo ""

# 1. 复制必要的文件到 Shared 项目（如果还没有）
SHARED_DIR="$PROJECT_ROOT/src/WO.Property.Shared"
if [ ! -f "$SHARED_DIR/Tenant/ITenantDbFactory.cs" ]; then
    echo "复制 Tenant 文件到 WO.Property.Shared..."
    mkdir -p "$SHARED_DIR/Tenant"
    cp "$PROJECT_ROOT/src/WO.Property.TicketService/Tenant/ITenantDbFactory.cs" "$SHARED_DIR/Tenant/"
fi

# 2. 创建 TenantDbFactory.cs（如果不存在）
if [ ! -f "$SHARED_DIR/Tenant/TenantDbFactory.cs" ]; then
    cat > "$SHARED_DIR/Tenant/TenantDbFactory.cs" << 'ENDFILE'
using System.Collections.Concurrent;

namespace WO.Property.Shared.Tenant;

public interface ITenantDbFactory
{
    string? GetCurrentTenantCode();
    void SetCurrentTenantCode(string tenantCode);
    void Clear();
    string GetTenantConnectionString(string tenantCode);
}

public class TenantDbFactory : ITenantDbFactory
{
    private readonly TenantConfigLoader _configLoader;
    private readonly ILogger<TenantDbFactory> _logger;
    private static readonly AsyncLocal<string?> _currentTenantCode = new();

    public TenantDbFactory(TenantConfigLoader configLoader, ILogger<TenantDbFactory> logger)
    {
        _configLoader = configLoader;
        _logger = logger;
    }

    public string? GetCurrentTenantCode() => _currentTenantCode.Value;

    public void SetCurrentTenantCode(string tenantCode)
    {
        if (!_configLoader.TenantExists(tenantCode))
        {
            _logger.LogWarning("租户 [{TenantCode}] 不存在于配置中", tenantCode);
            throw new InvalidOperationException($"租户 [{tenantCode}] 不存在");
        }
        _currentTenantCode.Value = tenantCode;
    }

    public void Clear() => _currentTenantCode.Value = null;

    public string GetTenantConnectionString(string tenantCode)
    {
        return _configLoader.BuildConnectionString(tenantCode);
    }
}
ENDFILE
fi

# 3. 为每个服务创建/更新 Program.cs
for service_info in "${SERVICES[@]}"; do
    IFS=':' read -r service_name port <<< "$service_info"
    service_dir="$PROJECT_ROOT/src/WO.Property.$service_name"
    
    echo "检查服务: $service_name ($port)"
    
    if [ ! -d "$service_dir" ]; then
        echo "  ⚠️ 目录不存在，跳过"
        continue
    fi
    
    # 检查是否已有 X-Project 支持
    if grep -q "X-Project\|TenantDbFactory\|ITenantDbFactory" "$service_dir/Program.cs" 2>/dev/null; then
        echo "  ✅ 已有 X-Project 支持"
        continue
    fi
    
    echo "  🔧 需要改造"
done

echo ""
echo "=== Phase 9 初步分析完成 ==="
echo ""
echo "下一步: 为每个服务添加:"
echo "1. Tenant/ITenantDbFactory.cs"
echo "2. Tenant/TenantDbFactory.cs"
echo "3. Tenant/TenantConfigLoader.cs"
echo "4. Middleware/TenantRoutingMiddleware.cs"
echo "5. 更新 Program.cs"
echo ""
echo "是否继续改造？"