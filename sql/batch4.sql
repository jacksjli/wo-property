-- WO-Property 34模块字段统一 - 批次4: 住户/房产(3) + 财务类(1-2)
-- RenovationRequests, FinanceRecords, PaymentRecords

-- RenovationRequests (renovation) - 11字段
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'applicant_name', '申请人姓名', 'string', 'RenovationRequests', 'renovation', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='applicant_name' AND Module='renovation');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'applicant_phone', '申请人电话', 'string', 'RenovationRequests', 'renovation', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='applicant_phone' AND Module='renovation');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'description', '描述', 'textarea', 'RenovationRequests', 'renovation', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='description' AND Module='renovation');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'start_date', '开始日期', 'date', 'RenovationRequests', 'renovation', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='start_date' AND Module='renovation');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'end_date', '结束日期', 'date', 'RenovationRequests', 'renovation', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='end_date' AND Module='renovation');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'status', '状态', 'select', 'RenovationRequests', 'renovation', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='status' AND Module='renovation');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'remark', '备注', 'textarea', 'RenovationRequests', 'renovation', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='remark' AND Module='renovation');

-- FinanceRecords (finance) - 18字段
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'record_number', '记录编号', 'string', 'FinanceRecords', 'finance', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='record_number' AND Module='finance');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'type', '类型', 'string', 'FinanceRecords', 'finance', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='type' AND Module='finance');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'category', '类别', 'string', 'FinanceRecords', 'finance', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='category' AND Module='finance');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'amount', '金额', 'number', 'FinanceRecords', 'finance', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='amount' AND Module='finance');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'balance', '余额', 'number', 'FinanceRecords', 'finance', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='balance' AND Module='finance');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'payment_method', '支付方式', 'string', 'FinanceRecords', 'finance', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='payment_method' AND Module='finance');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'record_date', '记录日期', 'date', 'FinanceRecords', 'finance', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='record_date' AND Module='finance');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'handler', '经手人', 'string', 'FinanceRecords', 'finance', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='handler' AND Module='finance');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'related_party', '相关方', 'string', 'FinanceRecords', 'finance', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='related_party' AND Module='finance');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'contract_no', '合同编号', 'string', 'FinanceRecords', 'finance', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='contract_no' AND Module='finance');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'bill_no', '账单编号', 'string', 'FinanceRecords', 'finance', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='bill_no' AND Module='finance');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'description', '描述', 'textarea', 'FinanceRecords', 'finance', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='description' AND Module='finance');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'receipt_no', '收据编号', 'string', 'FinanceRecords', 'finance', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='receipt_no' AND Module='finance');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'status', '状态', 'select', 'FinanceRecords', 'finance', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='status' AND Module='finance');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'remarks', '备注', 'textarea', 'FinanceRecords', 'finance', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='remarks' AND Module='finance');

-- PaymentRecords (payment) - 16字段
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'payment_number', '支付编号', 'string', 'PaymentRecords', 'payment', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='payment_number' AND Module='payment');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'payment_type', '支付类型', 'string', 'PaymentRecords', 'payment', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='payment_type' AND Module='payment');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'amount', '金额', 'number', 'PaymentRecords', 'payment', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='amount' AND Module='payment');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'period_start', '开始期间', 'date', 'PaymentRecords', 'payment', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='period_start' AND Module='payment');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'period_end', '结束期间', 'date', 'PaymentRecords', 'payment', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='period_end' AND Module='payment');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'due_date', '到期日期', 'date', 'PaymentRecords', 'payment', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='due_date' AND Module='payment');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'paid_date', '支付日期', 'date', 'PaymentRecords', 'payment', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='paid_date' AND Module='payment');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'status', '状态', 'select', 'PaymentRecords', 'payment', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='status' AND Module='payment');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'payment_method', '支付方式', 'string', 'PaymentRecords', 'payment', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='payment_method' AND Module='payment');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'transaction_id', '交易ID', 'string', 'PaymentRecords', 'payment', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='transaction_id' AND Module='payment');
INSERT IGNORE INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, Module, IsShared, Status, IsRequired)
SELECT 'remarks', '备注', 'textarea', 'PaymentRecords', 'payment', true, 'Active', false
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM FieldDefinitions WHERE FieldKey='remarks' AND Module='payment');

SELECT 'Batch 4 done: renovation, finance, payment' as msg;