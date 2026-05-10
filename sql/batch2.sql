-- WO-Property 34模块字段统一 - 批次2: 基础数据(5-9)
-- Departments, JobTypes, DeviceTypes, Suppliers, keys

-- Departments (department) - 5字段
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'name', '名称', 'string', 'Departments', 'department', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='name' AND Module='department');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'code', '编码', 'string', 'Departments', 'department', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='code' AND Module='department');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'description', '描述', 'string', 'Departments', 'department', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='description' AND Module='department');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'sort_order', '排序', 'number', 'Departments', 'department', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='sort_order' AND Module='department');

-- JobTypes (jobType) - 9字段
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'name', '名称', 'string', 'JobTypes', 'jobType', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='name' AND Module='jobType');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'code', '编码', 'string', 'JobTypes', 'jobType', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='code' AND Module='jobType');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'description', '描述', 'string', 'JobTypes', 'jobType', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='description' AND Module='jobType');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'category', '类别', 'string', 'JobTypes', 'jobType', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='category' AND Module='jobType');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'status', '状态', 'select', 'JobTypes', 'jobType', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='status' AND Module='jobType');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'sort_order', '排序', 'number', 'JobTypes', 'jobType', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='sort_order' AND Module='jobType');

-- DeviceTypes (deviceType) - 9字段
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'name', '名称', 'string', 'DeviceTypes', 'deviceType', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='name' AND Module='deviceType');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'code', '编码', 'string', 'DeviceTypes', 'deviceType', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='code' AND Module='deviceType');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'description', '描述', 'string', 'DeviceTypes', 'deviceType', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='description' AND Module='deviceType');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'category', '类别', 'string', 'DeviceTypes', 'deviceType', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='category' AND Module='deviceType');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'status', '状态', 'select', 'DeviceTypes', 'deviceType', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='status' AND Module='deviceType');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'sort_order', '排序', 'number', 'DeviceTypes', 'deviceType', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='sort_order' AND Module='deviceType');

-- Suppliers (supplier) - 10字段
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'name', '名称', 'string', 'Suppliers', 'supplier', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='name' AND Module='supplier');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'code', '编码', 'string', 'Suppliers', 'supplier', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='code' AND Module='supplier');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'contact_person', '联系人', 'string', 'Suppliers', 'supplier', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='contact_person' AND Module='supplier');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'contact_phone', '联系电话', 'string', 'Suppliers', 'supplier', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='contact_phone' AND Module='supplier');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'address', '地址', 'string', 'Suppliers', 'supplier', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='address' AND Module='supplier');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'type', '类型', 'string', 'Suppliers', 'supplier', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='type' AND Module='supplier');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'status', '状态', 'select', 'Suppliers', 'supplier', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='status' AND Module='supplier');

-- keys (key) - 18字段
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'key_no', '钥匙编号', 'string', 'keys', 'key', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='key_no' AND Module='key');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'name', '名称', 'string', 'keys', 'key', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='name' AND Module='key');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'type', '类型', 'string', 'keys', 'key', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='type' AND Module='key');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'location', '位置', 'string', 'keys', 'key', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='location' AND Module='key');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'building', '楼栋', 'string', 'keys', 'key', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='building' AND Module='key');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'floor', '楼层', 'string', 'keys', 'key', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='floor' AND Module='key');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'door_no', '门号', 'string', 'keys', 'key', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='door_no' AND Module='key');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'quantity', '数量', 'number', 'keys', 'key', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='quantity' AND Module='key');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'status', '状态', 'select', 'keys', 'key', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='status' AND Module='key');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'holder', '持有人', 'string', 'keys', 'key', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='holder' AND Module='key');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'holder_phone', '持有人电话', 'string', 'keys', 'key', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='holder_phone' AND Module='key');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'last_borrow_time', '最后借出时间', 'datetime', 'keys', 'key', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='last_borrow_time' AND Module='key');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'borrow_count', '借用次数', 'number', 'keys', 'key', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='borrow_count' AND Module='key');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'photo', '照片', 'string', 'keys', 'key', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='photo' AND Module='key');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'remark', '备注', 'textarea', 'keys', 'key', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='remark' AND Module='key');

SELECT 'Batch 2 done: department, jobType, deviceType, supplier, key' as msg;