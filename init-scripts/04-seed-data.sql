-- ============================================
-- 种子数据：测试用户和项目
-- BCrypt 哈希密码：Test@123
-- ============================================

USE center_db;

-- 插入租户
INSERT INTO tenants (tenant_code, tenant_name, db_name, status) VALUES
('tenant_a', '阳光物业', 'tenant_a', 'Active'),
('tenant_b', '绿城物业', 'tenant_b', 'Active');

-- 插入项目（租户A）
INSERT INTO projects (tenant_id, project_code, project_name, address, status) VALUES
(1, 'YGHY001', '阳光花园小区', '北京市朝阳区阳光路1号', 'Active'),
(1, 'YGXY001', '阳光新苑', '北京市朝阳区新光路2号', 'Active');

-- 插入项目（租户B）
INSERT INTO projects (tenant_id, project_code, project_name, address, status) VALUES
(2, 'LCGY001', '绿城公寓', '上海市浦东新区绿城路1号', 'Active'),
(2, 'LCXY001', '绿城馨苑', '上海市浦东新区新城路2号', 'Active');

-- 插入用户（租户A）
INSERT INTO users (tenant_id, username, password_hash, full_name, email, phone, role, status) VALUES
(1, 'admin_a', '$2b$11$fRS8KUane8okVAC8z3Zxl.F0Dp1MVDjbp6FSOOi0lBA8ZrB9xLkni', '阳光物业管理员', 'admin_a@yg.com', '13800138001', 'Administrator', 'Active'),
(1, 'tech_a', '$2b$11$fRS8KUane8okVAC8z3Zxl.F0Dp1MVDjbp6FSOOi0lBA8ZrB9xLkni', '阳光物业技术员', 'tech_a@yg.com', '13800138002', 'Technician', 'Active');

-- 插入用户（租户B）
INSERT INTO users (tenant_id, username, password_hash, full_name, email, phone, role, status) VALUES
(2, 'admin_b', '$2b$11$fRS8KUane8okVAC8z3Zxl.F0Dp1MVDjbp6FSOOi0lBA8ZrB9xLkni', '绿城物业管理员', 'admin_b@lc.com', '13800138003', 'Administrator', 'Active'),
(2, 'tech_b', '$2b$11$fRS8KUane8okVAC8z3Zxl.F0Dp1MVDjbp6FSOOi0lBA8ZrB9xLkni', '绿城物业技术员', 'tech_b@lc.com', '13800138004', 'Technician', 'Active');

-- 用户-项目关联
INSERT INTO user_projects (user_id, project_id, role_in_project) VALUES
(1, 1, 'Admin'),
(1, 2, 'Admin'),
(2, 1, 'Worker'),
(3, 3, 'Admin'),
(3, 4, 'Admin'),
(4, 3, 'Worker');

-- 租户A初始工单
USE tenant_a;
INSERT INTO tickets (ticket_code, title, description, category, priority, status, created_by, assigned_to, project_id) VALUES
('WO-20260518-0001', '电梯故障报修', '1号楼电梯突然停止运行', '设备维修', 'High', 'New', 1, 2, 1);

-- 租户B初始工单
USE tenant_b;
INSERT INTO tickets (ticket_code, title, description, category, priority, status, created_by, assigned_to, project_id) VALUES
('WO-20260518-0002', '水管漏水', '3单元水管漏水严重', '设备维修', 'High', 'New', 3, 4, 3);