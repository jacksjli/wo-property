-- ============================================
-- 租户B数据库初始化：tenant_b
-- Schema 与 tenant_a 相同
-- ============================================

USE tenant_b;

CREATE TABLE IF NOT EXISTS tickets (
    id INT AUTO_INCREMENT PRIMARY KEY,
    ticket_code VARCHAR(50) NOT NULL UNIQUE COMMENT '工单编号',
    title VARCHAR(200) NOT NULL COMMENT '工单标题',
    description VARCHAR(1000) NULL,
    category VARCHAR(50) NULL COMMENT '工单类别',
    priority VARCHAR(20) NOT NULL DEFAULT 'Medium' COMMENT 'High / Medium / Low',
    status VARCHAR(20) NOT NULL DEFAULT 'New' COMMENT 'New / Pending / InProgress / Completed / Closed',
    created_by INT NOT NULL COMMENT '创建人ID（来自 center_db.users）',
    assigned_to INT NULL COMMENT '指派人ID',
    project_id INT NOT NULL COMMENT '所属项目ID（来自 center_db.projects）',
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NULL ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_ticket_code (ticket_code),
    INDEX idx_status (status),
    INDEX idx_priority (priority),
    INDEX idx_category (category),
    INDEX idx_project_id (project_id),
    INDEX idx_created_at (created_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS buildings (
    id INT AUTO_INCREMENT PRIMARY KEY,
    project_id INT NOT NULL,
    building_code VARCHAR(50) NOT NULL COMMENT '楼栋编号',
    building_name VARCHAR(100) NOT NULL COMMENT '楼栋名称',
    floors INT NULL COMMENT '楼层数',
    remark VARCHAR(500) NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NULL ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_project_id (project_id),
    UNIQUE INDEX idx_project_building (project_id, building_code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
