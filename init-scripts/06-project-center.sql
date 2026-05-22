-- ================================================================
-- WO 物业管理软件 - 单租户多项目架构 - 中心库 Schema
-- 创建时间: 2026-05-20
-- 架构: project_center (共享元数据)
-- ================================================================

USE project_center;

-- ---------------------------------------------------------------
-- 1. 项目定义表
-- ---------------------------------------------------------------
DROP TABLE IF EXISTS projects;
CREATE TABLE projects (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    code VARCHAR(50) NOT NULL UNIQUE COMMENT '项目代码（英文唯一标识）',
    name VARCHAR(100) NOT NULL COMMENT '项目名称',
    database_name VARCHAR(50) NOT NULL UNIQUE COMMENT '对应数据库名',
    status ENUM('active','inactive','archived') DEFAULT 'active' COMMENT '状态',
    config JSON COMMENT '项目配置（扩展用）',
    description TEXT COMMENT '项目描述',
    address VARCHAR(200) COMMENT '项目地址',
    contact_phone VARCHAR(20) COMMENT '联系电话',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_status (status),
    INDEX idx_code (code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='项目定义表';

-- ---------------------------------------------------------------
-- 2. 项目成员表（用户-项目关系）
-- ---------------------------------------------------------------
DROP TABLE IF EXISTS project_members;
CREATE TABLE project_members (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT NOT NULL COMMENT '关联 users 表',
    project_code VARCHAR(50) NOT NULL COMMENT '关联 projects.code',
    role ENUM('admin','manager','operator','viewer') NOT NULL DEFAULT 'viewer' COMMENT '角色',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY uk_user_project (user_id, project_code),
    INDEX idx_project (project_code),
    FOREIGN KEY (project_code) REFERENCES projects(code) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='项目成员表';

-- ---------------------------------------------------------------
-- 3. 用户表（中心库统一用户）
-- ---------------------------------------------------------------
DROP TABLE IF EXISTS users;
CREATE TABLE users (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    username VARCHAR(50) NOT NULL UNIQUE COMMENT '用户名',
    password_hash VARCHAR(255) NOT NULL COMMENT '密码哈希',
    display_name VARCHAR(100) COMMENT '显示名称',
    email VARCHAR(100) COMMENT '邮箱',
    phone VARCHAR(20) COMMENT '电话',
    avatar VARCHAR(255) COMMENT '头像URL',
    status ENUM('active','inactive','deleted') DEFAULT 'active' COMMENT '状态',
    last_login_at DATETIME COMMENT '最后登录时间',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_username (username),
    INDEX idx_status (status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='用户表';

-- ---------------------------------------------------------------
-- 4. 共享字段定义（所有项目共用）
-- ---------------------------------------------------------------
DROP TABLE IF EXISTS shared_field_definitions;
CREATE TABLE shared_field_definitions (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    field_key VARCHAR(100) NOT NULL UNIQUE COMMENT '标准字段名',
    display_name VARCHAR(100) NOT NULL COMMENT '显示名',
    field_type VARCHAR(50) NOT NULL DEFAULT 'text' COMMENT '类型: text/number/date/select/boolean/textarea',
    source VARCHAR(100) COMMENT '来源模块',
    module VARCHAR(50) COMMENT '所属模块',
    options JSON COMMENT '下拉选项',
    is_required BOOLEAN DEFAULT FALSE COMMENT '是否必填',
    is_shared BOOLEAN DEFAULT TRUE COMMENT '是否共享',
    width INT DEFAULT 100 COMMENT '列宽',
    sort_order INT DEFAULT 0 COMMENT '排序',
    status ENUM('Active','Inactive') DEFAULT 'Active' COMMENT '状态',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_field_key (field_key),
    INDEX idx_source (source),
    INDEX idx_module (module),
    INDEX idx_status (status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='共享字段定义表';

-- ---------------------------------------------------------------
-- 5. 共享等价映射
-- ---------------------------------------------------------------
DROP TABLE IF EXISTS shared_field_equivalences;
CREATE TABLE shared_field_equivalences (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    canonical_field VARCHAR(100) NOT NULL COMMENT '标准字段名',
    equivalent_field VARCHAR(100) NOT NULL COMMENT '别名字段名',
    display_name VARCHAR(100) COMMENT '显示名',
    module VARCHAR(50) COMMENT '所属模块',
    field_type VARCHAR(50) DEFAULT 'string' COMMENT '字段类型',
    status ENUM('Active','Inactive') DEFAULT 'Active' COMMENT '状态',
    canonical_display_name VARCHAR(100) COMMENT '标准字段显示名',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY uk_equiv (canonical_field, equivalent_field, module),
    INDEX idx_canonical (canonical_field),
    INDEX idx_module (module),
    INDEX idx_status (status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='共享字段等价映射表';

-- ---------------------------------------------------------------
-- 6. 共享工种表
-- ---------------------------------------------------------------
DROP TABLE IF EXISTS shared_job_types;
CREATE TABLE shared_job_types (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    code VARCHAR(50) NOT NULL COMMENT '工种代码',
    name VARCHAR(100) NOT NULL COMMENT '工种名称',
    description TEXT COMMENT '工种描述',
    category VARCHAR(50) COMMENT '分类',
    status ENUM('Active','Inactive') DEFAULT 'Active' COMMENT '状态',
    sort_order INT DEFAULT 0 COMMENT '排序',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY uk_code (code),
    INDEX idx_category (category),
    INDEX idx_status (status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='共享工种表';

-- ---------------------------------------------------------------
-- 7. 共享部门表
-- ---------------------------------------------------------------
DROP TABLE IF EXISTS shared_departments;
CREATE TABLE shared_departments (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    code VARCHAR(50) NOT NULL COMMENT '部门代码',
    name VARCHAR(100) NOT NULL COMMENT '部门名称',
    parent_id BIGINT COMMENT '上级部门ID',
    level INT DEFAULT 1 COMMENT '层级',
    sort_order INT DEFAULT 0 COMMENT '排序',
    status ENUM('Active','Inactive') DEFAULT 'Active' COMMENT '状态',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY uk_code (code),
    INDEX idx_parent (parent_id),
    INDEX idx_status (status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='共享部门表';

-- ---------------------------------------------------------------
-- 8. 共享工单类型表
-- ---------------------------------------------------------------
DROP TABLE IF EXISTS shared_ticket_types;
CREATE TABLE shared_ticket_types (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    code VARCHAR(50) NOT NULL COMMENT '类型代码',
    name VARCHAR(100) NOT NULL COMMENT '类型名称',
    icon VARCHAR(50) DEFAULT 'Document' COMMENT '图标',
    color VARCHAR(20) DEFAULT '#409EFF' COMMENT '颜色',
    sort_order INT DEFAULT 0 COMMENT '排序',
    status ENUM('Active','Inactive') DEFAULT 'Active' COMMENT '状态',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY uk_code (code),
    INDEX idx_status (status),
    INDEX idx_sort (sort_order)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='共享工单类型表';

-- ---------------------------------------------------------------
-- 9. 共享人员表（可选，各项目也可独立）
-- ---------------------------------------------------------------
DROP TABLE IF EXISTS shared_personnel;
CREATE TABLE shared_personnel (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    employee_no VARCHAR(50) NOT NULL UNIQUE COMMENT '员工工号',
    name VARCHAR(100) NOT NULL COMMENT '姓名',
    phone VARCHAR(20) COMMENT '电话',
    email VARCHAR(100) COMMENT '邮箱',
    department VARCHAR(50) COMMENT '部门',
    position VARCHAR(50) COMMENT '职位',
    role VARCHAR(50) COMMENT '角色',
    status ENUM('active','inactive') DEFAULT 'active' COMMENT '状态',
    hire_date DATE COMMENT '入职日期',
    id_card VARCHAR(20) COMMENT '身份证',
    avatar VARCHAR(255) COMMENT '头像',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_employee_no (employee_no),
    INDEX idx_department (department),
    INDEX idx_status (status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='共享人员表';

-- ---------------------------------------------------------------
-- 初始化数据
-- ---------------------------------------------------------------

-- 插入默认项目
INSERT INTO projects (code, name, database_name, status, description) VALUES
('jinxiu', '锦绣花园', 'project_jinxiu', 'active', '第一个演示项目'),
('yanguang', '阳光小区', 'project_yanguang', 'active', '第二个演示项目'),
('xingfuli', '幸福里', 'project_xingfuli', 'active', '第三个演示项目');

-- 插入默认管理员
INSERT INTO users (username, password_hash, display_name, email, status) VALUES
('admin', '$2a$11$8K1p/a0dL1LXMIgou7j.u.JCJ7TMDj/Zjv9N9F1wWMIj2bP7F7E5a', '系统管理员', 'admin@example.com', 'active');

-- 插入项目成员关系
INSERT INTO project_members (user_id, project_code, role) VALUES
(1, 'jinxiu', 'admin'),
(1, 'yanguang', 'admin'),
(1, 'xingfuli', 'admin');

-- 插入共享工种
INSERT INTO shared_job_types (code, name, description, category) VALUES
('QIANGRUODIAN', '强弱电', '电气维修', '维修'),
('KONGTIAOTONGFENG', '空调通风', '空调和通风系统维修', '维修'),
('LOUSHUI', '漏水', '水管和防水维修', '维修'),
('PLUMBING', '给排水', '给排水系统维修', '维修'),
('ZONGHEWEIXIU', '综合维修', '综合性维修服务', '维修'),
('DIANTI', '电梯', '电梯维修保养', '维修'),
('ZHIANJIUFEN', '治安纠纷', '治安纠纷处理', '保安'),
('RENSHENWEIXIE', '人身威胁', '人身安全威胁处理', '保安'),
('CHELIANGJIUFEN', '车辆纠纷', '车辆相关纠纷处理', '保安'),
('DAILY_CLEANING', '日常保洁', '日常清洁服务', '保洁'),
('LAJIQINGLI', '垃圾清理', '垃圾清理和转运', '保洁'),
('DEEP_CLEANING', '深度保洁', '深度清洁服务', '保洁');

-- 插入共享部门
INSERT INTO shared_departments (code, name, parent_id, level, sort_order) VALUES
('ENGINEERING', '工程部', NULL, 1, 1),
('CUSTOMER_SERVICE', '客服部', NULL, 1, 2),
('SECURITY', '安保部', NULL, 1, 3),
('CLEANING', '保洁部', NULL, 1, 4),
('ADMIN', '行政部', NULL, 1, 5),
('FINANCE', '财务部', NULL, 1, 6);

-- 插入共享工单类型
INSERT INTO shared_ticket_types (code, name, icon, color, sort_order) VALUES
('REPAIR', '维修', 'Tools', '#409EFF', 1),
('SAFETY', '保安', 'Shield', '#F56C6C', 2),
('CONSULT', '咨询', 'QuestionFilled', '#67C23A', 3),
('URGENT', '应急', 'Lightning', '#E6A23C', 4),
('CLEANING', '保洁', 'Brush', '#909399', 5),
('COMPLAINT', '投诉', 'ChatDotRound', '#F56C6C', 6),
('ZHUANGXIU', '装修', 'House', '#409EFF', 0),
('TEST', '测试类型', 'Document', '#409EFF', 0);