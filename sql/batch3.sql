-- WO-Property 34模块字段统一 - 批次3: 住户/房产(1-2)
-- Residents, ParkingRecords

-- Residents (resident) - 12字段
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'name', '姓名', 'string', 'Residents', 'resident', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='name' AND Module='resident');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'phone', '电话', 'string', 'Residents', 'resident', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='phone' AND Module='resident');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'id_card_number', '身份证号', 'string', 'Residents', 'resident', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='id_card_number' AND Module='resident');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'resident_type', '住户类型', 'string', 'Residents', 'resident', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='resident_type' AND Module='resident');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'check_in_date', '入住日期', 'date', 'Residents', 'resident', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='check_in_date' AND Module='resident');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'status', '状态', 'select', 'Residents', 'resident', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='status' AND Module='resident');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'remark', '备注', 'textarea', 'Residents', 'resident', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='remark' AND Module='resident');

-- ParkingRecords (parking) - 14字段
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'parking_space_number', '车位编号', 'string', 'ParkingRecords', 'parking', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='parking_space_number' AND Module='parking');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'floor', '楼层', 'number', 'ParkingRecords', 'parking', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='floor' AND Module='parking');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'space_type', '车位类型', 'string', 'ParkingRecords', 'parking', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='space_type' AND Module='parking');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'license_plate', '车牌号', 'string', 'ParkingRecords', 'parking', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='license_plate' AND Module='parking');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'start_date', '开始日期', 'date', 'ParkingRecords', 'parking', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='start_date' AND Module='parking');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'end_date', '结束日期', 'date', 'ParkingRecords', 'parking', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='end_date' AND Module='parking');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'monthly_fee', '月费', 'number', 'ParkingRecords', 'parking', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='monthly_fee' AND Module='parking');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'status', '状态', 'select', 'ParkingRecords', 'parking', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='status' AND Module='parking');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'remark', '备注', 'textarea', 'ParkingRecords', 'parking', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='remark' AND Module='parking');

SELECT 'Batch 3 done: resident, parking' as msg;