-- ============================================
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