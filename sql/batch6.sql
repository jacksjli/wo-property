-- WO-Property 34模块字段统一 - 批次6: 工单任务(2-5)
-- InspectionRecords, dispatch_rules, timeout_rules, ticket_types

-- InspectionRecords (inspection) - 14字段
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'inspection_title', '巡检标题', 'string', 'InspectionRecords', 'inspection', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='inspection_title' AND Module='inspection');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'inspection_area', '巡检区域', 'string', 'InspectionRecords', 'inspection', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='inspection_area' AND Module='inspection');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'inspector_name', '巡检员姓名', 'string', 'InspectionRecords', 'inspection', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='inspector_name' AND Module='inspection');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'inspection_date', '巡检日期', 'date', 'InspectionRecords', 'inspection', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='inspection_date' AND Module='inspection');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'inspection_time', '巡检时间', 'string', 'InspectionRecords', 'inspection', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='inspection_time' AND Module='inspection');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'status', '状态', 'select', 'InspectionRecords', 'inspection', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='status' AND Module='inspection');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'result', '结果', 'string', 'InspectionRecords', 'inspection', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='result' AND Module='inspection');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'findings', '发现', 'textarea', 'InspectionRecords', 'inspection', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='findings' AND Module='inspection');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'next_inspection_date', '下次巡检日期', 'date', 'InspectionRecords', 'inspection', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='next_inspection_date' AND Module='inspection');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'remarks', '备注', 'textarea', 'InspectionRecords', 'inspection', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='remarks' AND Module='inspection');

-- dispatch_rules (dispatch) - 27字段
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'rule_no', '规则编号', 'string', 'dispatch_rules', 'dispatch', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='rule_no' AND Module='dispatch');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'name', '名称', 'string', 'dispatch_rules', 'dispatch', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='name' AND Module='dispatch');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'description', '描述', 'textarea', 'dispatch_rules', 'dispatch', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='description' AND Module='dispatch');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'type', '类型', 'string', 'dispatch_rules', 'dispatch', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='type' AND Module='dispatch');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'ticket_color', '工单颜色', 'string', 'dispatch_rules', 'dispatch', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='ticket_color' AND Module='dispatch');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'location', '位置', 'string', 'dispatch_rules', 'dispatch', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='location' AND Module='dispatch');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'location_type', '位置类型', 'string', 'dispatch_rules', 'dispatch', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='location_type' AND Module='dispatch');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'priority', '优先级', 'number', 'dispatch_rules', 'dispatch', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='priority' AND Module='dispatch');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'auto_assign', '自动指派', 'boolean', 'dispatch_rules', 'dispatch', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='auto_assign' AND Module='dispatch');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'notify_backup', '通知备岗', 'boolean', 'dispatch_rules', 'dispatch', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='notify_backup' AND Module='dispatch');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'allow_transfer', '允许转派', 'boolean', 'dispatch_rules', 'dispatch', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='allow_transfer' AND Module='dispatch');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'timeout_escalation', '超时升级', 'boolean', 'dispatch_rules', 'dispatch', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='timeout_escalation' AND Module='dispatch');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'operator_ids', '操作员IDs', 'textarea', 'dispatch_rules', 'dispatch', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='operator_ids' AND Module='dispatch');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'supervisor_id', '主管ID', 'string', 'dispatch_rules', 'dispatch', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='supervisor_id' AND Module='dispatch');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'manager_id', '经理ID', 'string', 'dispatch_rules', 'dispatch', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='manager_id' AND Module='dispatch');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'department_head_id', '部门主管ID', 'string', 'dispatch_rules', 'dispatch', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='department_head_id' AND Module='dispatch');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'company_head_id', '公司主管ID', 'string', 'dispatch_rules', 'dispatch', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='company_head_id' AND Module='dispatch');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'backup_ids', '备岗IDs', 'textarea', 'dispatch_rules', 'dispatch', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='backup_ids' AND Module='dispatch');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'notify_methods', '通知方式', 'string', 'dispatch_rules', 'dispatch', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='notify_methods' AND Module='dispatch');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'status', '状态', 'select', 'dispatch_rules', 'dispatch', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='status' AND Module='dispatch');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'match_count', '匹配次数', 'number', 'dispatch_rules', 'dispatch', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='match_count' AND Module='dispatch');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'success_count', '成功次数', 'number', 'dispatch_rules', 'dispatch', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='success_count' AND Module='dispatch');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'avg_response_time', '平均响应时间', 'number', 'dispatch_rules', 'dispatch', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='avg_response_time' AND Module='dispatch');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'remark', '备注', 'textarea', 'dispatch_rules', 'dispatch', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='remark' AND Module='dispatch');

-- timeout_rules (timeout) - 7字段
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'color', '颜色', 'string', 'timeout_rules', 'timeout', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='color' AND Module='timeout');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'role', '角色', 'string', 'timeout_rules', 'timeout', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='role' AND Module='timeout');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'hours', '小时数', 'number', 'timeout_rules', 'timeout', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='hours' AND Module='timeout');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'enabled', '是否启用', 'boolean', 'timeout_rules', 'timeout', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='enabled' AND Module='timeout');

-- ticket_types (ticketType) - 7字段
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'name', '名称', 'string', 'ticket_types', 'ticketType', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='name' AND Module='ticketType');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'code', '编码', 'string', 'ticket_types', 'ticketType', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='code' AND Module='ticketType');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'status', '状态', 'select', 'ticket_types', 'ticketType', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='status' AND Module='ticketType');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'sort_order', '排序', 'number', 'ticket_types', 'ticketType', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='sort_order' AND Module='ticketType');

SELECT 'Batch 6 done: inspection, dispatch, timeout, ticketType' as msg;