-- 装修申请表
CREATE TABLE IF NOT EXISTS RenovationRequests (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    RoomId INT NOT NULL,
    ApplicantName VARCHAR(50) NOT NULL,
    ApplicantPhone VARCHAR(20),
    Description TEXT NOT NULL,
    StartDate DATE,
    EndDate DATE,
    Status VARCHAR(20) DEFAULT 'pending',
    Remarks TEXT,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (RoomId) REFERENCES Rooms(Id)
);

-- 种子数据：10条装修申请（覆盖不同状态）
INSERT INTO RenovationRequests (RoomId, ApplicantName, ApplicantPhone, Description, StartDate, EndDate, Status, Remarks) VALUES
(1, '张三', '13800138001', '厨房整体翻新，更换橱柜和瓷砖', '2026-05-15', '2026-05-30', 'pending', '需要物业审批'),
(2, '李四', '13800138002', '卫生间装修，更换马桶和洗手盆', '2026-05-10', '2026-05-20', 'approved', '已批准，注意施工时间'),
(3, '王五', '13900139003', '客厅地板更换，强化木地板', '2026-04-20', '2026-05-05', 'completed', '已完成验收'),
(4, '赵六', '13700137004', '卧室墙面刷新', '2026-05-12', '2026-05-18', 'pending', NULL),
(5, '钱七', '13600136005', '阳台封闭工程', '2026-05-01', '2026-05-25', 'rejected', '不符合小区规定，已驳回'),
(6, '孙八', '13500135006', '全屋水电改造', '2026-04-25', '2026-05-15', 'completed', '验收通过'),
(7, '周九', '13400134007', '书房定制家具安装', '2026-05-20', '2026-05-22', 'pending', NULL),
(8, '吴十', '13300133008', '空调外机位置调整', '2026-05-08', '2026-05-10', 'approved', '已批准'),
(9, '郑一', '13200132009', '窗户更换为隔音窗', '2026-05-18', '2026-05-28', 'pending', NULL),
(10, '冯二', '13100131010', '地下室防水处理', '2026-04-15', '2026-05-10', 'completed', '已验收');