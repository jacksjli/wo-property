-- WO-Property 34模块字段统一 - 批次8: 物流/生活(3) + 人员/系统(1-2)
-- TakeoutOrders, Visitors, Personnel, Roles, RolePermissions

-- TakeoutOrders (delivery) - 15字段
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'order_no', '订单编号', 'string', 'TakeoutOrders', 'delivery', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='order_no' AND Module='delivery');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'restaurant_name', '餐厅名称', 'string', 'TakeoutOrders', 'delivery', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='restaurant_name' AND Module='delivery');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'food_type', '食物类型', 'string', 'TakeoutOrders', 'delivery', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='food_type' AND Module='delivery');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'resident_name', '住户姓名', 'string', 'TakeoutOrders', 'delivery', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='resident_name' AND Module='delivery');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'room_no', '房号', 'string', 'TakeoutOrders', 'delivery', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='room_no' AND Module='delivery');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'phone', '电话', 'string', 'TakeoutOrders', 'delivery', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='phone' AND Module='delivery');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'delivery_person', '配送员', 'string', 'TakeoutOrders', 'delivery', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='delivery_person' AND Module='delivery');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'delivery_phone', '配送员电话', 'string', 'TakeoutOrders', 'delivery', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='delivery_phone' AND Module='delivery');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'delivery_time', '送达时间', 'datetime', 'TakeoutOrders', 'delivery', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='delivery_time' AND Module='delivery');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'status', '状态', 'select', 'TakeoutOrders', 'delivery', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='status' AND Module='delivery');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'total_amount', '总金额', 'number', 'TakeoutOrders', 'delivery', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='total_amount' AND Module='delivery');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'remark', '备注', 'textarea', 'TakeoutOrders', 'delivery', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='remark' AND Module='delivery');

-- Visitors (visitor) - 14字段
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'visitor_name', '访客姓名', 'string', 'Visitors', 'visitor', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='visitor_name' AND Module='visitor');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'visitor_phone', '访客电话', 'string', 'Visitors', 'visitor', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='visitor_phone' AND Module='visitor');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'id_card_number', '身份证号', 'string', 'Visitors', 'visitor', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='id_card_number' AND Module='visitor');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'visit_purpose', '访问目的', 'string', 'Visitors', 'visitor', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='visit_purpose' AND Module='visitor');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'visit_date', '访问日期', 'date', 'Visitors', 'visitor', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='visit_date' AND Module='visitor');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'visit_time', '访问时间', 'string', 'Visitors', 'visitor', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='visit_time' AND Module='visitor');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'leave_time', '离开时间', 'string', 'Visitors', 'visitor', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='leave_time' AND Module='visitor');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'host_name', '受访人姓名', 'string', 'Visitors', 'visitor', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='host_name' AND Module='visitor');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'host_phone', '受访人电话', 'string', 'Visitors', 'visitor', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='host_phone' AND Module='visitor');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'status', '状态', 'select', 'Visitors', 'visitor', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='status' AND Module='visitor');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'remarks', '备注', 'textarea', 'Visitors', 'visitor', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='remarks' AND Module='visitor');

-- Personnel (personnel) - 38字段 (跳过外键字段)
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'employee_no', '员工工号', 'string', 'Personnel', 'personnel', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='employee_no' AND Module='personnel');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'name', '姓名', 'string', 'Personnel', 'personnel', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='name' AND Module='personnel');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'avatar', '头像', 'string', 'Personnel', 'personnel', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='avatar' AND Module='personnel');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'gender', '性别', 'string', 'Personnel', 'personnel', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='gender' AND Module='personnel');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'birthday', '生日', 'date', 'Personnel', 'personnel', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='birthday' AND Module='personnel');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'id_card', '身份证', 'string', 'Personnel', 'personnel', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='id_card' AND Module='personnel');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'phone', '电话', 'string', 'Personnel', 'personnel', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='phone' AND Module='personnel');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'email', '邮箱', 'string', 'Personnel', 'personnel', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='email' AND Module='personnel');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'address', '地址', 'string', 'Personnel', 'personnel', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='address' AND Module='personnel');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'education', '学历', 'string', 'Personnel', 'personnel', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='education' AND Module='personnel');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'graduate_school', '毕业学校', 'string', 'Personnel', 'personnel', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='graduate_school' AND Module='personnel');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'major', '专业', 'string', 'Personnel', 'personnel', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='major' AND Module='personnel');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'role', '角色', 'string', 'Personnel', 'personnel', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='role' AND Module='personnel');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'department_name', '部门名称', 'string', 'Personnel', 'personnel', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='department_name' AND Module='personnel');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'position', '职位', 'string', 'Personnel', 'personnel', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='position' AND Module='personnel');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'employment_type', '用工类型', 'string', 'Personnel', 'personnel', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='employment_type' AND Module='personnel');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'hire_date', '入职日期', 'date', 'Personnel', 'personnel', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='hire_date' AND Module='personnel');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'contract_start', '合同开始', 'date', 'Personnel', 'personnel', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='contract_start' AND Module='personnel');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'contract_end', '合同结束', 'date', 'Personnel', 'personnel', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='contract_end' AND Module='personnel');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'salary', '薪资', 'number', 'Personnel', 'personnel', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='salary' AND Module='personnel');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'bank_account', '银行账号', 'string', 'Personnel', 'personnel', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='bank_account' AND Module='personnel');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'social_security_no', '社保号', 'string', 'Personnel', 'personnel', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='social_security_no' AND Module='personnel');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'specialties', '特长', 'textarea', 'Personnel', 'personnel', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='specialties' AND Module='personnel');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'backups', '备岗', 'textarea', 'Personnel', 'personnel', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='backups' AND Module='personnel');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'emergency_contact_name', '紧急联系人', 'string', 'Personnel', 'personnel', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='emergency_contact_name' AND Module='personnel');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'emergency_contact_relationship', '紧急联系人关系', 'string', 'Personnel', 'personnel', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='emergency_contact_relationship' AND Module='personnel');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'emergency_contact_phone', '紧急联系人电话', 'string', 'Personnel', 'personnel', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='emergency_contact_phone' AND Module='personnel');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'attendance_count', '出勤次数', 'number', 'Personnel', 'personnel', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='attendance_count' AND Module='personnel');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'overtime_hours', '加班时长', 'number', 'Personnel', 'personnel', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='overtime_hours' AND Module='personnel');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'leave_days', '请假天数', 'number', 'Personnel', 'personnel', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='leave_days' AND Module='personnel');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'performance_score', '绩效评分', 'number', 'Personnel', 'personnel', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='performance_score' AND Module='personnel');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'training_count', '培训次数', 'number', 'Personnel', 'personnel', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='training_count' AND Module='personnel');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'status', '状态', 'select', 'Personnel', 'personnel', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='status' AND Module='personnel');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'remark', '备注', 'textarea', 'Personnel', 'personnel', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='remark' AND Module='personnel');

-- Roles (accessControl) - 5字段
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'name', '名称', 'string', 'Roles', 'accessControl', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='name' AND Module='accessControl');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'code', '编码', 'string', 'Roles', 'accessControl', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='code' AND Module='accessControl');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'level', '级别', 'number', 'Roles', 'accessControl', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='level' AND Module='accessControl');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'description', '描述', 'string', 'Roles', 'accessControl', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='description' AND Module='accessControl');

-- RolePermissions (accessControl) - 9字段
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'role_code', '角色编码', 'string', 'RolePermissions', 'accessControl', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='role_code' AND Module='accessControl');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'module_key', '模块KEY', 'string', 'RolePermissions', 'accessControl', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='module_key' AND Module='accessControl');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'can_view', '可查看', 'boolean', 'RolePermissions', 'accessControl', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='can_view' AND Module='accessControl');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'can_create', '可创建', 'boolean', 'RolePermissions', 'accessControl', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='can_create' AND Module='accessControl');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'can_edit', '可编辑', 'boolean', 'RolePermissions', 'accessControl', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='can_edit' AND Module='accessControl');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'can_delete', '可删除', 'boolean', 'RolePermissions', 'accessControl', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='can_delete' AND Module='accessControl');

SELECT 'Batch 8 done: delivery, visitor, personnel, accessControl' as msg;