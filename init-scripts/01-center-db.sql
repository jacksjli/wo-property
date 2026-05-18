-- ============================================
-- 中心库初始化：center_db
-- Phase 0 多物业分库改造
-- ============================================

USE center_db;

-- 租户表
CREATE TABLE IF NOT EXISTS tenants (
    id INT AUTO_INCREMENT PRIMARY KEY,
    tenant_code VARCHAR(50) NOT NULL UNIQUE COMMENT '租户编码：tenant_a / tenant_b',
    tenant_name VARCHAR(100) NOT NULL COMMENT '租户名称',
    db_name VARCHAR(50) NOT NULL COMMENT '对应的租户数据库名',
    status VARCHAR(20) NOT NULL DEFAULT 'Active' COMMENT 'Active / Inactive',
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NULL ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 项目表
CREATE TABLE IF NOT EXISTS projects (
    id INT AUTO_INCREMENT PRIMARY KEY,
    tenant_id INT NOT NULL,
    project_code VARCHAR(50) NOT NULL UNIQUE COMMENT '项目编码',
    project_name VARCHAR(100) NOT NULL COMMENT '项目名称',
    address VARCHAR(200) NULL COMMENT '项目地址',
    status VARCHAR(20) NOT NULL DEFAULT 'Active',
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NULL ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (tenant_id) REFERENCES tenants(id),
    INDEX idx_tenant_id (tenant_id),
    INDEX idx_project_code (project_code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 用户表（统一认证）
CREATE TABLE IF NOT EXISTS users (
    id INT AUTO_INCREMENT PRIMARY KEY,
    tenant_id INT NOT NULL,
    username VARCHAR(50) NOT NULL,
    password_hash VARCHAR(200) NOT NULL COMMENT 'BCrypt加密密码',
    full_name VARCHAR(100) NOT NULL COMMENT '姓名',
    email VARCHAR(100) NULL,
    phone VARCHAR(20) NULL,
    role VARCHAR(20) NOT NULL DEFAULT 'User' COMMENT 'Administrator / Technician / User',
    status VARCHAR(20) NOT NULL DEFAULT 'Active' COMMENT 'Active / Inactive',
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NULL ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (tenant_id) REFERENCES tenants(id),
    UNIQUE INDEX idx_username (username),
    INDEX idx_tenant_id (tenant_id),
    INDEX idx_status (status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 用户-项目关联表
CREATE TABLE IF NOT EXISTS user_projects (
    user_id INT NOT NULL,
    project_id INT NOT NULL,
    role_in_project VARCHAR(20) NOT NULL DEFAULT 'Worker' COMMENT 'Admin / Manager / Worker',
    joined_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (user_id, project_id),
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE,
    INDEX idx_user_id (user_id),
    INDEX idx_project_id (project_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;