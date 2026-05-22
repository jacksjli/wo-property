#!/bin/bash
# Phase 6: 创建 project_jinxiu 数据库并迁移 wo_property 数据

set -e

DB_SERVER="127.0.0.1"
DB_USER="root"
DB_PASS=""

echo "=== Phase 6: 数据迁移 ==="
echo ""

# 1. 创建 project_jinxiu 数据库
echo "1. 创建 project_jinxiu 数据库..."
mysql -u root -h $DB_SERVER -e "CREATE DATABASE IF NOT EXISTS project_jinxiu CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;" 2>&1

# 2. 创建 Tickets 表（与 wo_property 一致）
echo "2. 创建 Tickets 表..."
mysql -u root -h $DB_SERVER project_jinxiu << 'EOF'
CREATE TABLE IF NOT EXISTS tickets (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id BIGINT NOT NULL DEFAULT 1,
  TicketNumber VARCHAR(20) NOT NULL UNIQUE,
  ticket_no VARCHAR(20),
  Title VARCHAR(100) NOT NULL,
  Description TEXT,
  TicketType VARCHAR(20),
  category VARCHAR(50),
  ticket_type_id BIGINT,
  Priority VARCHAR(10) DEFAULT 'normal',
  color VARCHAR(10) DEFAULT 'blue',
  Location VARCHAR(200),
  location_detail VARCHAR(500),
  area_id BIGINT,
  images TEXT,
  rating INT,
  Status VARCHAR(20) DEFAULT 'pending',
  current_role VARCHAR(20) DEFAULT 'operator',
  escalation_level INT DEFAULT 0,
  dispatch_status VARCHAR(20) DEFAULT 'pending',
  reject_count INT DEFAULT 0,
  survey_status VARCHAR(20) DEFAULT 'pending',
  last_escalated_at DATETIME,
  custom_fields JSON,
  ReporterName VARCHAR(50),
  ReporterPhone VARCHAR(20),
  creator_id INT NOT NULL,
  assignee_id INT,
  HandlerId INT,
  AssignedTo VARCHAR(50),
  BuildingId INT,
  RoomId INT,
  Source VARCHAR(20) DEFAULT 'user_report',
  Pictures TEXT,
  Remarks TEXT,
  created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
  UpdatedAt DATETIME,
  ContactPersonName VARCHAR(100),
  ContactPhone VARCHAR(20),
  project_id INT DEFAULT 1,
  updated_at DATETIME ON UPDATE CURRENT_TIMESTAMP,
  INDEX idx_category (category),
  INDEX idx_status (Status),
  INDEX idx_priority (Priority),
  INDEX idx_created_at (created_at),
  INDEX idx_project_id (project_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
EOF

echo "表创建完成"

# 3. 迁移 wo_property Tickets 数据
echo "3. 迁移 wo_property Tickets 数据 (39条)..."

# 先确认 wo_property Tickets 列
echo "源表列:"
mysql -u root -h $DB_SERVER wo_property -e "DESCRIBE Tickets" 2>&1 | awk '{print $1}' | tr '\n' ','

echo "" && echo "迁移数据..."
mysql -u root -h $DB_SERVER wo_property -e "
INSERT INTO project_jinxiu.tickets (
  Id, tenant_id, TicketNumber, ticket_no, Title, Description, TicketType, category,
  ticket_type_id, Priority, color, Location, location_detail, area_id,
  images, rating, Status, current_role, escalation_level, dispatch_status,
  reject_count, survey_status, last_escalated_at, custom_fields,
  ReporterName, ReporterPhone, creator_id, assignee_id, HandlerId,
  AssignedTo, BuildingId, RoomId, Source, Pictures, Remarks,
  created_at, UpdatedAt, ContactPersonName, ContactPhone, project_id
)
SELECT 
  Id, tenant_id, TicketNumber, ticket_no, Title, Description, TicketType, category,
  ticket_type_id, Priority, color, Location, location_detail, area_id,
  images, rating, Status, current_role, escalation_level, dispatch_status,
  reject_count, survey_status, last_escalated_at, custom_fields,
  ReporterName, ReporterPhone, creator_id, assignee_id, HandlerId,
  AssignedTo, BuildingId, RoomId, Source, Pictures, Remarks,
  created_at, UpdatedAt, ContactPersonName, ContactPhone, project_id
FROM Tickets
WHERE project_id = 1 OR project_id IS NULL OR project_id = 1;
" 2>&1

echo "" && echo "验证数据..." && \
mysql -u root -h $DB_SERVER project_jinxiu -e "SELECT COUNT(*) as ticket_count FROM tickets;" 2>&1

echo "" && echo "=== Phase 6 第一阶段完成 ==="
echo "project_jinxiu 数据库已创建，数据已迁移"