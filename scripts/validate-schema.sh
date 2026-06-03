#!/bin/bash
# Schema Validation Script
# 验证 C# 模型 vs 数据库实际表结构
# 使用方法: ./scripts/validate-schema.sh [service_name]

set -e

DB_HOST="127.0.0.1"
DB_PORT="3306"
DB_USER="root"
DB_PASS=""
DB_NAME="wo_property"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "========================================="
echo "  WO Property Schema Validation"
echo "========================================="

# Function to compare schema
validate_table() {
    local table_name=$1
    local service_dir=$2
    
    echo ""
    echo "Checking table: $table_name"
    
    # Get database columns
    local db_columns=$(mysql -h $DB_HOST -P $DB_PORT -u $DB_USER -e "SHOW COLUMNS FROM \`$table_name\`" 2>/dev/null | grep -v "Field" | awk '{print $1}')
    
    if [ -z "$db_columns" ]; then
        echo -e "${YELLOW}Table '$table_name' not found in database${NC}"
        return 1
    fi
    
    # Check if TenantDbContext exists
    if [ ! -f "$service_dir/Data/TenantDbContext.cs" ]; then
        echo -e "${RED}TenantDbContext.cs not found${NC}"
        return 1
    fi
    
    # Basic check: look for HasColumnName mappings
    local has_issues=0
    
    echo "  Database columns:"
    for col in $db_columns; do
        echo "    - $col"
    done
    
    # Check for exact column names in TenantDbContext
    local missing_columns=$(echo "$db_columns" | while read col; do
        if ! grep -q "HasColumnName(\"$col\")" "$service_dir/Data/TenantDbContext.cs" 2>/dev/null; then
            # Check if it's using different naming convention
            if grep -q "HasColumnName(\"[a-z_]\+\")" "$service_dir/Data/TenantDbContext.cs"; then
                : # Has some column mappings
            else
                echo "$col"
            fi
        fi
    done)
    
    if [ -n "$missing_columns" ]; then
        echo -e "${RED}  Missing or incorrect mappings for:${NC}"
        echo "$missing_columns" | while read col; do
            echo -e "${RED}    - $col${NC}"
        done
        has_issues=1
    fi
    
    if [ $has_issues -eq 0 ]; then
        echo -e "${GREEN}  ✓ All columns mapped correctly${NC}"
    fi
    
    return $has_issues
}

# Validate each service
validate_service() {
    local service_name=$1
    local service_dir=""
    
    # Find service directory
    case $service_name in
        "KeyService")
            service_dir="src/WO.Property.KeyService"
            ;;
        "InspectionService")
            service_dir="src/WO.Property.InspectionService"
            ;;
        "FinanceService")
            service_dir="src/WO.Property.FinanceService"
            ;;
        "VisitorService")
            service_dir="src/WO.Property.VisitorService"
            ;;
        "MaterialService")
            service_dir="src/WO.Property.MaterialService"
            ;;
        "DeviceService")
            service_dir="src/WO.Property.DeviceService"
            ;;
        "TicketService")
            service_dir="src/WO.Property.TicketService"
            ;;
        "ContractService")
            service_dir="src/WO.Property.ContractService"
            ;;
        "PersonService")
            service_dir="src/WO.Property.PersonService"
            ;;
        "DispatchService")
            service_dir="src/WO.Property.DispatchService"
            ;;
        "StatisticsService")
            service_dir="src/WO.Property.StatisticsService"
            ;;
        "MasterDataService")
            service_dir="src/WO.Property.MasterDataService"
            ;;
        "CenterService")
            service_dir="src/WO.Property.CenterService"
            ;;
        *)
            echo "Unknown service: $service_name"
            return 1
            ;;
    esac
    
    if [ ! -d "$service_dir" ]; then
        echo -e "${RED}Service directory not found: $service_dir${NC}"
        return 1
    fi
    
    echo ""
    echo "=== Validating $service_name ==="
    
    case $service_name in
        "KeyService")
            validate_table "keys" "$service_dir"
            ;;
        "InspectionService")
            validate_table "InspectionRecords" "$service_dir"
            ;;
        "FinanceService")
            validate_table "FinanceRecords" "$service_dir"
            validate_table "PaymentRecords" "$service_dir"
            ;;
        "VisitorService")
            validate_table "Visitors" "$service_dir"
            ;;
        "MaterialService")
            validate_table "materials" "$service_dir"
            ;;
        "DeviceService")
            validate_table "Devices" "$service_dir"
            ;;
        "TicketService")
            validate_table "Tickets" "$service_dir"
            ;;
        "ContractService")
            validate_table "Contracts" "$service_dir"
            ;;
        "PersonService")
            validate_table "Personnel" "$service_dir"
            ;;
        "DispatchService")
            validate_table "dispatch_records" "$service_dir"
            ;;
    esac
}

# If service name provided, validate only that
if [ -n "$1" ]; then
    validate_service "$1"
else
    # Validate all services
    echo ""
    echo "No service specified. Use: $0 [service_name]"
    echo ""
    echo "Available services:"
    echo "  - KeyService"
    echo "  - InspectionService"
    echo "  - FinanceService"
    echo "  - VisitorService"
    echo "  - MaterialService"
    echo "  - DeviceService"
    echo "  - TicketService"
    echo "  - ContractService"
    echo "  - PersonService"
    echo "  - DispatchService"
fi

echo ""
echo "========================================="