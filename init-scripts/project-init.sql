-- ============================================
-- WO Property - 新项目数据库初始化脚本
-- 创建日期: 2026-05-21
-- 适用: 新项目创建时自动执行
-- ============================================

-- 1. 工单模块 (tickets)
CREATE TABLE IF NOT EXISTS tickets (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    tenant_id BIGINT DEFAULT 1,
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
    creator_id INT,
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

-- 2. 工单类型表 (ticket_types)
CREATE TABLE IF NOT EXISTS ticket_types (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Code VARCHAR(20) NOT NULL UNIQUE,
    Name VARCHAR(50) NOT NULL,
    Description VARCHAR(200),
    Color VARCHAR(10) DEFAULT '#409eff',
    SortOrder INT DEFAULT 0,
    Status VARCHAR(20) DEFAULT 'active',
    project_id INT DEFAULT 1,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_project (project_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 3. 区域表 (areas)
CREATE TABLE IF NOT EXISTS areas (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Code VARCHAR(20) NOT NULL UNIQUE,
    Name VARCHAR(50) NOT NULL,
    ParentId INT DEFAULT 0,
    Level INT DEFAULT 1,
    SortOrder INT DEFAULT 0,
    Status VARCHAR(20) DEFAULT 'active',
    project_id INT DEFAULT 1,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_parent (ParentId),
    INDEX idx_project (project_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 4. 楼栋表 (buildings)
CREATE TABLE IF NOT EXISTS buildings (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Code VARCHAR(20) NOT NULL UNIQUE,
    Name VARCHAR(50) NOT NULL,
    AreaId INT,
    FloorCount INT DEFAULT 1,
    Description VARCHAR(200),
    Status VARCHAR(20) DEFAULT 'active',
    project_id INT DEFAULT 1,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_area (AreaId),
    INDEX idx_project (project_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 5. 房号表 (rooms)
CREATE TABLE IF NOT EXISTS rooms (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Code VARCHAR(20) NOT NULL UNIQUE,
    BuildingId INT,
    Floor INT DEFAULT 1,
    Unit VARCHAR(10),
    RoomNumber VARCHAR(20),
    Area DECIMAL(10,2),
    Type VARCHAR(20) DEFAULT 'residential',
    Status VARCHAR(20) DEFAULT 'active',
    project_id INT DEFAULT 1,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_building (BuildingId),
    INDEX idx_project (project_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 6. 部门表 (departments)
CREATE TABLE IF NOT EXISTS departments (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Code VARCHAR(20) NOT NULL UNIQUE,
    Name VARCHAR(50) NOT NULL,
    ParentId INT DEFAULT 0,
    ManagerId INT,
    Description VARCHAR(200),
    SortOrder INT DEFAULT 0,
    Status VARCHAR(20) DEFAULT 'active',
    project_id INT DEFAULT 1,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_parent (ParentId),
    INDEX idx_project (project_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 7. 员工表 (persons - 整合自 PersonService)
CREATE TABLE IF NOT EXISTS persons (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Code VARCHAR(20) NOT NULL UNIQUE,
    Name VARCHAR(50) NOT NULL,
    Gender VARCHAR(10),
    Phone VARCHAR(20),
    Email VARCHAR(100),
    DepartmentId INT,
    Role VARCHAR(50),
    IdCard VARCHAR(20),
    Status VARCHAR(20) DEFAULT 'active',
    project_id INT DEFAULT 1,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_department (DepartmentId),
    INDEX idx_project (project_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 8. 设备表 (devices)
CREATE TABLE IF NOT EXISTS devices (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Code VARCHAR(50) NOT NULL UNIQUE,
    Name VARCHAR(100) NOT NULL,
    Type VARCHAR(50),
    Model VARCHAR(100),
    Location VARCHAR(200),
    BuildingId INT,
    Floor INT,
    InstallDate DATE,
    Status VARCHAR(20) DEFAULT 'active',
    Description TEXT,
    project_id INT DEFAULT 1,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_type (Type),
    INDEX idx_location (Location),
    INDEX idx_project (project_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 9. 物料表 (materials)
CREATE TABLE IF NOT EXISTS materials (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Code VARCHAR(50) NOT NULL UNIQUE,
    Name VARCHAR(100) NOT NULL,
    Category VARCHAR(50),
    Unit VARCHAR(20),
    Stock INT DEFAULT 0,
    MinStock INT DEFAULT 0,
    Price DECIMAL(10,2),
    Status VARCHAR(20) DEFAULT 'active',
    project_id INT DEFAULT 1,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_category (Category),
    INDEX idx_project (project_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 10. 通知公告表 (notifications)
CREATE TABLE IF NOT EXISTS notifications (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Title VARCHAR(200) NOT NULL,
    Content TEXT,
    Type VARCHAR(20) DEFAULT 'system',
    Priority VARCHAR(10) DEFAULT 'normal',
    Status VARCHAR(20) DEFAULT 'draft',
    PublishedAt DATETIME,
    ExpiresAt DATETIME,
    creator_id INT,
    project_id INT DEFAULT 1,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_status (Status),
    INDEX idx_project (project_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 11. 合同表 (contracts)
CREATE TABLE IF NOT EXISTS contracts (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    ContractNo VARCHAR(50) NOT NULL UNIQUE,
    Title VARCHAR(200) NOT NULL,
    Type VARCHAR(50),
    PartyA VARCHAR(100),
    PartyB VARCHAR(100),
    Amount DECIMAL(15,2),
    StartDate DATE,
    EndDate DATE,
    Status VARCHAR(20) DEFAULT 'active',
    project_id INT DEFAULT 1,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_type (Type),
    INDEX idx_project (project_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 12. 财务表 (finances)
CREATE TABLE IF NOT EXISTS finances (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Type VARCHAR(20) NOT NULL,
    Category VARCHAR(50),
    Amount DECIMAL(15,2) NOT NULL,
    Payer VARCHAR(100),
    Payee VARCHAR(100),
    PaymentMethod VARCHAR(20),
    Status VARCHAR(20) DEFAULT 'pending',
    Remark TEXT,
    operator_id INT,
    project_id INT DEFAULT 1,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_type (Type),
    INDEX idx_status (Status),
    INDEX idx_project (project_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 13. 巡检表 (inspections)
CREATE TABLE IF NOT EXISTS inspections (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Title VARCHAR(200) NOT NULL,
    Type VARCHAR(50),
    Location VARCHAR(200),
    BuildingId INT,
    PlanDate DATETIME,
    InspectorId INT,
    Status VARCHAR(20) DEFAULT 'pending',
    Result TEXT,
    project_id INT DEFAULT 1,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_status (Status),
    INDEX idx_project (project_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 14. 钥匙表 (keys)
CREATE TABLE IF NOT EXISTS `keys` (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Code VARCHAR(50) NOT NULL UNIQUE,
    Name VARCHAR(100) NOT NULL,
    Type VARCHAR(50),
    Location VARCHAR(200),
    BuildingId INT,
    Status VARCHAR(20) DEFAULT 'available',
    HolderId INT,
    project_id INT DEFAULT 1,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_status (Status),
    INDEX idx_project (project_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 15. 访客表 (visitors)
CREATE TABLE IF NOT EXISTS visitors (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(50) NOT NULL,
    Phone VARCHAR(20),
    IdCard VARCHAR(20),
    VisitPurpose VARCHAR(100),
    VisitTime DATETIME,
    LeaveTime DATETIME,
    HostName VARCHAR(50),
    HostPhone VARCHAR(20),
    Status VARCHAR(20) DEFAULT 'visiting',
    project_id INT DEFAULT 1,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_status (Status),
    INDEX idx_project (project_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 16. 投诉表 (complaints)
CREATE TABLE IF NOT EXISTS complaints (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Title VARCHAR(200) NOT NULL,
    Content TEXT,
    Type VARCHAR(50),
    Source VARCHAR(20) DEFAULT 'user_report',
    Status VARCHAR(20) DEFAULT 'pending',
    HandlerId INT,
    Result TEXT,
    project_id INT DEFAULT 1,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_status (Status),
    INDEX idx_project (project_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 17. 车位表 (parking)
CREATE TABLE IF NOT EXISTS parking (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Code VARCHAR(20) NOT NULL UNIQUE,
    Location VARCHAR(100),
    Type VARCHAR(20) DEFAULT 'car',
    Status VARCHAR(20) DEFAULT 'available',
    project_id INT DEFAULT 1,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_status (Status),
    INDEX idx_project (project_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- ============================================
-- 基础数据初始化
-- ============================================

-- 插入默认工单类型
INSERT INTO ticket_types (Code, Name, Description, Color, SortOrder) VALUES
('general', '一般', '一般工单', '#409eff', 1),
('urgent', '紧急', '紧急工单', '#f56c6c', 2),
('complaint', '投诉', '投诉工单', '#e6a23c', 3);

-- 插入默认区域
INSERT INTO areas (Code, Name, ParentId, Level) VALUES
('AREA001', '东区', 0, 1),
('AREA002', '西区', 0, 1);

-- 插入默认部门
INSERT INTO departments (Code, Name, ParentId, SortOrder) VALUES
('DEPT001', '物业部', 0, 1),
('DEPT002', '工程部', 0, 2),
('DEPT003', '客服部', 0, 3);

-- 插入默认工种
INSERT INTO persons (Code, Name, Gender, Role, Status) VALUES
('WORKER001', '维修工A', '男', '维修工', 'active'),
('WORKER002', '保洁员A', '女', '保洁员', 'active');-- ============================================
-- 扩展基础数据初始化 (2026-05-21)
-- ============================================

-- 插入更多工单类型
INSERT INTO ticket_types (Code, Name, Description, Color, SortOrder) VALUES
('repair', '维修', '设备维修工单', '#67c23a', 4),
('maintenance', '保养', '设备保养工单', '#909399', 5),
('inspection', '巡检', '日常巡检工单', '#009688', 6),
('emergency', '应急', '应急处理工单', '#f56c6c', 7),
('consultation', '咨询', '咨询类工单', '#909399', 8);

-- 插入更多区域
INSERT INTO areas (Code, Name, ParentId, Level) VALUES
('AREA003', '南区', 0, 1),
('AREA004', '北区', 0, 1),
('AREA001-A', '东区-A区', 1, 2),
('AREA001-B', '东区-B区', 1, 2),
('AREA002-A', '西区-A区', 2, 2);

-- 插入更多部门（完善）
INSERT INTO departments (Code, Name, ParentId, SortOrder, Description) VALUES
('DEPT004', '保安部', 0, 4, '负责小区安全管理'),
('DEPT005', '保洁部', 0, 5, '负责小区清洁卫生'),
('DEPT006', '绿化部', 0, 6, '负责小区绿化养护'),
('DEPT007', '客服部', 0, 7, '负责客户接待与投诉处理'),
('DEPT008', '财务部', 0, 8, '负责财务管理与收费'),
('DEPT009', '行政部', 0, 9, '负责行政事务');

-- 插入工种定义（person Role 字段）
-- 注意：工种信息存储在 persons 表的 Role 字段
INSERT INTO persons (Code, Name, Gender, Role, Status, Phone) VALUES
('TECH001', '张师傅', '男', '电工', 'active', '138-0001-0001'),
('TECH002', '李师傅', '男', '水工', 'active', '138-0002-0002'),
('TECH003', '王师傅', '男', '空调工', 'active', '138-0003-0003'),
('TECH004', '刘师傅', '男', '电梯工', 'active', '138-0004-0004'),
('TECH005', '陈师傅', '男', '消防工', 'active', '138-0005-0005'),
('CLEAN001', '周保洁', '女', '保洁员', 'active', '139-0001-0001'),
('CLEAN002', '吴保洁', '女', '保洁员', 'active', '139-0002-0002'),
('SEC001', '赵保安', '男', '保安', 'active', '137-0001-0001'),
('SEC002', '钱保安', '男', '保安', 'active', '137-0002-0002'),
('SEC003', '孙保安', '男', '保安队长', 'active', '137-0003-0003'),
('GARDEN001', '周园丁', '男', '绿化工', 'active', '136-0001-0001'),
('GARDEN002', '吴园丁', '男', '绿化工', 'active', '136-0002-0002');

-- 插入供应商
CREATE TABLE IF NOT EXISTS suppliers (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Code VARCHAR(20) NOT NULL UNIQUE,
    Name VARCHAR(100) NOT NULL,
    ContactPerson VARCHAR(50),
    Phone VARCHAR(20),
    Address VARCHAR(200),
    Type VARCHAR(50),
    Status VARCHAR(20) DEFAULT 'active',
    project_id INT DEFAULT 1,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_type (Type),
    INDEX idx_project (project_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

INSERT INTO suppliers (Code, Name, ContactPerson, Phone, Type, Status) VALUES
('SUP001', '电梯维保公司', '张三', '150-0001-0001', '电梯维保', 'active'),
('SUP002', '消防维保公司', '李四', '150-0002-0002', '消防维保', 'active'),
('SUP003', '空调维修公司', '王五', '150-0003-0003', '空调维修', 'active'),
('SUP004', '保洁用品供应商', '赵六', '150-0004-0004', '保洁用品', 'active'),
('SUP005', '绿化养护公司', '钱七', '150-0005-0005', '绿化养护', 'active'),
('SUP006', '监控设备供应商', '孙八', '150-0006-0006', '设备供应', 'active'),
('SUP007', '门禁系统供应商', '周九', '150-0007-0007', '设备供应', 'active'),
('SUP008', '水电维修材料商', '吴十', '150-0008-0008', '维修材料', 'active');

-- 插入设备类型
CREATE TABLE IF NOT EXISTS device_types (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Code VARCHAR(20) NOT NULL UNIQUE,
    Name VARCHAR(50) NOT NULL,
    Description VARCHAR(200),
    MaintenanceCycle INT DEFAULT 30,
    Status VARCHAR(20) DEFAULT 'active',
    project_id INT DEFAULT 1,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

INSERT INTO device_types (Code, Name, Description, MaintenanceCycle) VALUES
('ELEVATOR', '电梯', '乘客电梯、货梯', 15),
('FIRE_SYS', '消防系统', '消防泵、喷淋、消火栓', 30),
('SURVEILLANCE', '监控系统', '摄像头、录像机、监控屏', 90),
('ACCESS_CTRL', '门禁系统', '道闸、门禁、人行通道', 30),
('PARKING_SYS', '停车场系统', '道闸、收费系统、车位引导', 30),
('HVAC', '暖通空调', '中央空调、风机盘管', 90),
('LIGHTING', '照明系统', '公共照明、景观照明', 90),
('WATER_SYS', '供水系统', '水泵、水箱、阀门', 30),
('ELECTRIC', '配电系统', '配电柜、开关箱、电表', 60),
('GAS_SYS', '燃气系统', '燃气表、阀门、管道', 90);

-- 插入楼栋示例数据
INSERT INTO buildings (Code, Name, AreaId, FloorCount, Description) VALUES
('BLD001', '1号楼', 1, 18, '高层住宅'),
('BLD002', '2号楼', 1, 18, '高层住宅'),
('BLD003', '3号楼', 1, 12, '小高层住宅'),
('BLD004', '5号楼', 2, 12, '小高层住宅'),
('BLD005', '6号楼', 2, 18, '高层住宅');

-- 插入房号示例数据
INSERT INTO rooms (Code, BuildingId, Floor, Unit, RoomNumber, Area, Type) VALUES
('ROOM001', 1, 1, 'A', '101', 89.5, 'residential'),
('ROOM002', 1, 1, 'A', '102', 120.3, 'residential'),
('ROOM003', 1, 2, 'A', '201', 89.5, 'residential'),
('ROOM004', 1, 2, 'A', '202', 120.3, 'residential'),
('ROOM005', 2, 1, 'B', '101', 95.0, 'residential'),
('ROOM006', 2, 1, 'B', '102', 110.0, 'residential'),
('ROOM007', 3, 1, 'C', '101', 85.0, 'residential'),
('ROOM008', 3, 2, 'C', '201', 85.0, 'residential');

-- 插入系统配置
CREATE TABLE IF NOT EXISTS system_configs (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    ConfigKey VARCHAR(50) NOT NULL UNIQUE,
    ConfigValue TEXT,
    Description VARCHAR(200),
    GroupName VARCHAR(50) DEFAULT 'general',
    Status VARCHAR(20) DEFAULT 'active',
    project_id INT DEFAULT 1,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_group (GroupName)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

INSERT INTO system_configs (ConfigKey, ConfigValue, Description, GroupName) VALUES
('company_name', '锦绣花园物业服务中心', '公司名称', 'basic'),
('company_phone', '400-888-8888', '服务热线', 'basic'),
('company_address', '某市某区某街道123号', '公司地址', 'basic'),
('work_start_hour', '08:30', '上班时间', 'work'),
('work_end_hour', '17:30', '下班时间', 'work'),
('ticket_response_hours', '24', '工单响应时限(小时)', 'ticket'),
('ticket_resolve_hours', '72', '工单解决时限(小时)', 'ticket'),
('auto_dispatch_enabled', 'true', '自动派单启用', 'dispatch'),
('overdue_warning_hours', '2', '超时预警时限(小时)', 'ticket');

-- ============================================
-- 初始化完成
-- ============================================