-- =====================================================
-- WO Property Management - 快递/清洁/社区/配送 迁移脚本
-- 版本: 1.0.0
-- 日期: 2026-05-09
-- 描述: 创建快递管理、清洁管理、社区管理、配送管理表及种子数据
-- =====================================================

-- 快递记录表
CREATE TABLE IF NOT EXISTS ExpressRecords (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    RoomId INT NOT NULL,
    RecipientName VARCHAR(50) NOT NULL,
    RecipientPhone VARCHAR(20),
    CourierCompany VARCHAR(50),
    TrackingNumber VARCHAR(100),
    PickupCode VARCHAR(20),
    Status VARCHAR(20) DEFAULT 'pending',
    PickupTime DATETIME,
    Remarks TEXT,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (RoomId) REFERENCES Rooms(Id)
);

-- 清洁记录表
CREATE TABLE IF NOT EXISTS CleaningRecords (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    BuildingId INT NOT NULL,
    CleaningArea VARCHAR(100),
    CleanerName VARCHAR(50),
    CleaningType VARCHAR(20),
    PlanDate DATE,
    ActualDate DATE,
    Status VARCHAR(20) DEFAULT 'pending',
    QualityLevel VARCHAR(10),
    Remarks TEXT,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (BuildingId) REFERENCES Buildings(Id)
);

-- 社区活动表
CREATE TABLE IF NOT EXISTS CommunityActivities (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    ActivityTitle VARCHAR(100) NOT NULL,
    ActivityType VARCHAR(20),
    Description TEXT,
    Organizer VARCHAR(50),
    Location VARCHAR(100),
    StartTime DATETIME,
    EndTime DATETIME,
    Status VARCHAR(20) DEFAULT 'planned',
    ParticipantCount INT DEFAULT 0,
    MaxParticipants INT,
    Remarks TEXT,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP
);

-- 配送请求表
CREATE TABLE IF NOT EXISTS DeliveryRequests (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    RoomId INT NOT NULL,
    ResidentName VARCHAR(50) NOT NULL,
    ResidentPhone VARCHAR(20),
    DeliveryCompany VARCHAR(50),
    DeliveryType VARCHAR(20),
    ItemDescription TEXT,
    Status VARCHAR(20) DEFAULT 'pending',
    DeliveryTime DATETIME,
    Remarks TEXT,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (RoomId) REFERENCES Rooms(Id)
);

-- =====================================================
-- 种子数据 - 快递记录 (10条)
-- =====================================================

INSERT INTO ExpressRecords (RoomId, RecipientName, RecipientPhone, CourierCompany, TrackingNumber, PickupCode, Status, Remarks, CreatedAt) VALUES
(1, '张伟', '13812340001', '顺丰速运', 'SF1234567890', 'A-001', 'picked', '已签收', '2026-05-01 09:00:00'),
(2, '李娜', '13812340002', '圆通速递', 'YT9876543210', 'A-002', 'picked', '业主已取', '2026-05-02 10:30:00'),
(3, '王强', '13812340003', '中通快递', 'ZT5555666677', 'A-003', 'informed', '已短信通知', '2026-05-05 14:00:00'),
(4, '赵敏', '13812340004', '韵达快递', 'YD3333444455', 'B-001', 'informed', '电话通知中', '2026-05-06 11:00:00'),
(5, '陈静', '13812340005', '申通快递', 'ST7777888899', 'B-002', 'pending', '等待通知', '2026-05-07 16:00:00'),
(6, '刘洋', '13812340006', '京东物流', 'JD1111222233', 'C-001', 'pending', '快递柜待取', '2026-05-08 09:30:00'),
(7, '周涛', '13812340007', '顺丰速运', 'SF4444555566', 'C-002', 'pending', '大件需自取', '2026-05-08 13:45:00'),
(8, '吴婷', '13812340008', '邮政EMS', 'EMS8888999900', 'D-001', 'returned', '超时退回', '2026-05-03 10:00:00'),
(9, '孙磊', '13812340009', '圆通速递', 'YT2222333344', 'D-002', 'informed', '已通知待取', '2026-05-09 08:00:00'),
(10, '郑云', '13812340010', '中通快递', 'ZT6666777788', 'D-003', 'pending', '新到快递', '2026-05-09 15:00:00');

-- =====================================================
-- 种子数据 - 清洁记录 (10条)
-- =====================================================

INSERT INTO CleaningRecords (BuildingId, CleaningArea, CleanerName, CleaningType, PlanDate, ActualDate, Status, QualityLevel, Remarks, CreatedAt) VALUES
(1, '1号楼大厅及电梯', '李保洁', '日常保洁', '2026-05-01', '2026-05-01', 'completed', '5', '厅面整洁', '2026-04-28 10:00:00'),
(2, '2号楼公共区域', '王保洁', '日常保洁', '2026-05-02', '2026-05-02', 'completed', '4', '地面需加强', '2026-04-29 10:00:00'),
(3, '3号楼地下车库', '张保洁', '深度清洁', '2026-05-03', '2026-05-03', 'quality_issue', '2', '油污清理不彻底', '2026-04-30 10:00:00'),
(4, '小区绿化带', '刘保洁', '日常保洁', '2026-05-04', '2026-05-04', 'completed', '5', '修剪整齐', '2026-05-01 10:00:00'),
(5, '5号楼楼道', '赵保洁', '日常保洁', '2026-05-05', NULL, 'in_progress', NULL, '正在进行中', '2026-05-02 10:00:00'),
(1, '会所及健身房', '孙保洁', '深度清洁', '2026-05-06', NULL, 'pending', NULL, '计划本周清洁', '2026-05-03 10:00:00'),
(2, '儿童游乐区', '周保洁', '消杀服务', '2026-05-07', NULL, 'pending', NULL, '消毒杀虫', '2026-05-04 10:00:00'),
(3, '垃圾分类房', '吴保洁', '日常保洁', '2026-05-08', NULL, 'pending', NULL, '每日例行', '2026-05-05 10:00:00'),
(4, '6号楼大堂', '郑保洁', '日常保洁', '2026-05-09', NULL, 'pending', NULL, '待执行', '2026-05-06 10:00:00'),
(5, '小区道路', '冯保洁', '日常保洁', '2026-05-10', NULL, 'pending', NULL, '计划周末', '2026-05-07 10:00:00');

-- =====================================================
-- 种子数据 - 社区活动 (8条)
-- =====================================================

INSERT INTO CommunityActivities (ActivityTitle, ActivityType, Description, Organizer, Location, StartTime, EndTime, Status, ParticipantCount, MaxParticipants, Remarks, CreatedAt) VALUES
('端午节包粽子活动', '节日庆祝', '邀请业主一起包粽子，感受传统节日氛围', '物业服务中心', '小区活动中心', '2026-05-28 09:00:00', '2026-05-28 12:00:00', 'planned', 0, 50, '准备糯米、粽叶等材料', '2026-05-01 10:00:00'),
('儿童绘画比赛', '文体活动', '面向小区儿童举办绘画比赛，主题：我爱我家', '业委会', '小区花园广场', '2026-05-15 14:00:00', '2026-05-15 17:00:00', 'ongoing', 28, 40, '设置一等奖1名、二等奖3名', '2026-05-01 10:00:00'),
('健康义诊活动', '健康讲座', '邀请三甲医院医生为业主提供免费健康咨询', '物业服务中心', '小区会所', '2026-05-10 09:00:00', '2026-05-10 12:00:00', 'completed', 65, 80, '测量血压、血糖等', '2026-04-20 10:00:00'),
('业主乒乓球比赛', '文体活动', '小区乒乓球爱好者友谊赛，设男女单打', '体育协会', '小区乒乓球室', '2026-04-25 09:00:00', '2026-04-25 18:00:00', 'completed', 32, 32, '圆满结束', '2026-04-10 10:00:00'),
('垃圾分类宣讲会', '教育培训', '邀请环保局专家讲解垃圾分类知识', '物业服务中心', '小区会议室', '2026-05-20 14:00:00', '2026-05-20 16:00:00', 'planned', 0, 60, '发放分类指南', '2026-05-05 10:00:00'),
('母亲节亲子活动', '节日庆祝', 'DIY母亲节礼物，增进亲子感情', '妇委会', '小区活动室', '2026-05-08 10:00:00', '2026-05-08 12:00:00', 'ongoing', 45, 50, '准备手工材料', '2026-04-25 10:00:00'),
('夏季防暑讲座', '健康讲座', '邀请医生讲解夏季防暑降温知识', '物业服务中心', '小区会所', '2026-05-25 15:00:00', '2026-05-25 17:00:00', 'planned', 0, 100, '发放防暑降温物品', '2026-05-06 10:00:00'),
('消防演练活动', '公益活动', '年度消防演练，提高业主安全意识', '安保部', '小区中心广场', '2026-04-15 09:00:00', '2026-04-15 11:00:00', 'cancelled', 0, 200, '因天气原因取消', '2026-04-01 10:00:00');

-- =====================================================
-- 种子数据 - 配送请求 (10条)
-- =====================================================

INSERT INTO DeliveryRequests (RoomId, ResidentName, ResidentPhone, DeliveryCompany, DeliveryType, ItemDescription, Status, DeliveryTime, Remarks, CreatedAt) VALUES
(1, '张伟', '13812340001', '美团', '外卖配送', '午餐：宫保鸡丁、米饭', 'delivered', '2026-05-01 12:30:00', '送至门口', '2026-05-01 11:45:00'),
(2, '李娜', '13812340002', '饿了么', '外卖配送', '下午茶：奶茶、蛋糕', 'delivered', '2026-05-02 15:00:00', '已签收', '2026-05-02 14:20:00'),
(3, '王强', '13812340003', '京东', '快递配送', '网购商品：运动鞋', 'in_transit', NULL, '配送员正在配送中', '2026-05-05 10:00:00'),
(4, '赵敏', '13812340004', '顺丰', '快递配送', '生鲜：樱桃2斤', 'in_transit', NULL, '注意保鲜', '2026-05-06 09:00:00'),
(5, '陈静', '13812340005', '美团', '外卖配送', '晚餐：火锅套餐', 'pending', NULL, '等待骑手接单', '2026-05-07 17:00:00'),
(6, '刘洋', '13812340006', '天猫超市', '快递配送', '日用品：洗衣液、纸巾', 'pending', NULL, '待配送', '2026-05-08 08:00:00'),
(7, '周涛', '13812340007', '饿了么', '外卖配送', '午餐：披萨、饮料', 'delivered', '2026-05-08 12:45:00', '客户已取', '2026-05-08 11:50:00'),
(8, '吴婷', '13812340008', '叮咚买菜', '生鲜配送', '新鲜蔬菜：西红柿、黄瓜、青菜', 'failed', NULL, '客户不在家，配送失败', '2026-05-03 10:30:00'),
(9, '孙磊', '13812340009', '美团', '外卖配送', '夜宵：烧烤、啤酒', 'pending', NULL, '备注加辣', '2026-05-09 21:00:00'),
(10, '郑云', '13812340010', '京东', '药品配送', '外卖：感冒药、退烧贴', 'delivered', '2026-05-09 10:00:00', '紧急配送已送达', '2026-05-09 08:30:00');

-- =====================================================
-- 验证查询
-- =====================================================

-- SELECT '快递记录总数: ' || COUNT(*) FROM ExpressRecords;
-- SELECT '清洁记录总数: ' || COUNT(*) FROM CleaningRecords;
-- SELECT '社区活动总数: ' || COUNT(*) FROM CommunityActivities;
-- SELECT '配送请求总数: ' || COUNT(*) FROM DeliveryRequests;
