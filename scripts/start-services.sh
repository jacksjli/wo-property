#!/bin/bash
# WO Property Management - Service Startup Script
# Development Environment

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"

echo "=========================================="
echo "  WO Property Management"
echo "  Service Startup Script"
echo "=========================================="

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to check if port is in use
check_port() {
    lsof -i :"$1" >/dev/null 2>&1
}

# Function to wait for service
wait_for_service() {
    local host=$1
    local port=$2
    local service=$3
    local max_attempts=30
    local attempt=1

    echo -n "Waiting for $service on $host:$port "
    while [ $attempt -le $max_attempts ]; do
        if curl -s -f "http://$host:$port/health" > /dev/null 2>&1; then
            echo -e "${GREEN}✓${NC}"
            return 0
        fi
        echo -n "."
        sleep 1
        attempt=$((attempt + 1))
    done
    echo -e "${RED}✗${NC}"
    echo "ERROR: $service failed to start"
    return 1
}

# Stop existing services
stop_services() {
    echo -e "\n${YELLOW}Stopping existing services...${NC}"

    # Stop local .NET services
    pkill -f "WO.Property.GatewayService" 2>/dev/null || true
    pkill -f "WO.Property.TicketService" 2>/dev/null || true
    pkill -f "WO.Property.AuthService" 2>/dev/null || true
    pkill -f "WO.Property.MasterDataService" 2>/dev/null || true
    pkill -f "WO.Property.PersonService" 2>/dev/null || true

    sleep 2
}

# Start MySQL (if not running)
start_mysql() {
    echo -e "\n${YELLOW}Checking MySQL...${NC}"
    if check_port 3306; then
        echo -e "${GREEN}MySQL already running on port 3306${NC}"
    else
        echo "Starting MySQL..."
        # MySQL might be installed via Homebrew or native
        if command -v mysql.server &> /dev/null; then
            mysql.server start
        elif command -v brew &> /dev/null; then
            brew services start mysql
        else
            echo -e "${RED}MySQL not found. Please install MySQL.${NC}"
            return 1
        fi
    fi
}

# Start PostgreSQL (if not running)
start_postgres() {
    echo -e "\n${YELLOW}Checking PostgreSQL...${NC}"
    if check_port 5432; then
        echo -e "${GREEN}PostgreSQL already running on port 5432${NC}"
    else
        echo "Starting PostgreSQL..."
        if command -v brew &> /dev/null; then
            brew services start postgresql@16 2>/dev/null || brew services start postgresql 2>/dev/null || true
        fi
        # Try to start via docker if available
        docker start wo-property-postgres 2>/dev/null || true
    fi
}

# Start Redis (if not running)
start_redis() {
    echo -e "\n${YELLOW}Checking Redis...${NC}"
    if check_port 6379; then
        echo -e "${GREEN}Redis already running on port 6379${NC}"
    else
        echo "Starting Redis..."
        if command -v brew &> /dev/null; then
            brew services start redis 2>/dev/null || true
        fi
        docker start wo-property-redis 2>/dev/null || true
    fi
}

# Start API Gateway
start_gateway() {
    echo -e "\n${YELLOW}Starting API Gateway (port 5000)...${NC}"
    cd "$PROJECT_DIR/src/WO.Property.GatewayService"
    nohup dotnet run -c Release --urls="http://0.0.0.0:5000" > /tmp/gateway.log 2>&1 &
    echo "Gateway PID: $!"
}

# Start AuthService
start_auth() {
    echo -e "\n${YELLOW}Starting AuthService (port 5106)...${NC}"
    cd "$PROJECT_DIR/src/WO.Property.AuthService"
    nohup dotnet run -c Release --urls="http://0.0.0.0:5106" > /tmp/auth.log 2>&1 &
    echo "AuthService PID: $!"
}

# Start TicketService
start_ticket() {
    echo -e "\n${YELLOW}Starting TicketService (port 5102)...${NC}"
    cd "$PROJECT_DIR/src/WO.Property.TicketService"
    nohup dotnet run -c Release --urls="http://0.0.0.0:5102" > /tmp/ticket.log 2>&1 &
    echo "TicketService PID: $!"
}

# Start MasterDataService
start_master() {
    echo -e "\n${YELLOW}Starting MasterDataService (port 5019)...${NC}"
    cd "$PROJECT_DIR/src/WO.Property.MasterDataService"
    nohup dotnet run -c Release --urls="http://0.0.0.0:5019" > /tmp/master.log 2>&1 &
    echo "MasterDataService PID: $!"
}

# Start PersonService
start_person() {
    echo -e "\n${YELLOW}Starting PersonService (port 5018)...${NC}"
    cd "$PROJECT_DIR/src/WO.Property.PersonService"
    nohup dotnet run -c Release --urls="http://0.0.0.0:5018" > /tmp/person.log 2>&1 &
    echo "PersonService PID: $!"
}

# Verify services
verify_services() {
    echo -e "\n${YELLOW}Verifying services...${NC}"

    wait_for_service localhost 5000 "API Gateway"
    wait_for_service localhost 5106 "AuthService"
    wait_for_service localhost 5102 "TicketService"
    wait_for_service localhost 5019 "MasterDataService"
    wait_for_service localhost 5018 "PersonService"
}

# Main
main() {
    stop_services

    # Start databases
    start_mysql
    start_postgres
    start_redis

    # Start services
    start_gateway
    start_auth
    start_ticket
    start_master
    start_person

    # Wait and verify
    echo -e "\n${YELLOW}Waiting for services to be ready...${NC}"
    sleep 5
    verify_services

    echo -e "\n${GREEN}=========================================="
    echo "  All services started successfully!"
    echo "==========================================${NC}"
    echo ""
    echo "Services:"
    echo "  - API Gateway:     http://localhost:5000"
    echo "  - AuthService:    http://localhost:5106"
    echo "  - TicketService:  http://localhost:5102"
    echo "  - MasterDataSvc:  http://localhost:5019"
    echo "  - PersonService:  http://localhost:5018"
    echo ""
    echo "Frontend (dev): npm run dev"
    echo "  - http://localhost:5173/project"
    echo ""
}

main "$@"