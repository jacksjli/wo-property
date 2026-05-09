-- =====================================================
-- WO Property Management - MasterDataService 迁移脚本
-- 版本: 1.0.0
-- 日期: 2026-05-05
-- 描述: 创建字段管理相关表
-- =====================================================

-- 创建 FieldDefinitions 表
CREATE TABLE IF NOT EXISTS FieldDefinitions (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    FieldKey VARCHAR(50) NOT NULL UNIQUE,
    DisplayName VARCHAR(100) NOT NULL,
    FieldType VARCHAR(20) NOT NULL DEFAULT 'text',
    Source VARCHAR(50) NOT NULL,
    IsShared BOOLEAN DEFAULT 0,
    Module VARCHAR(50),
    Options TEXT,
    DefaultValue TEXT,
    IsRequired BOOLEAN DEFAULT 0,
    Width INTEGER DEFAULT 100,
    SortOrder INTEGER DEFAULT 0,
    Status VARCHAR(20) DEFAULT 'Active',
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME,

    INDEX idx_fieldKey (FieldKey),
    INDEX idx_module (Module),
    INDEX idx_isShared (IsShared),
    INDEX idx_status (Status)
);

-- 创建 ModuleFields 表
CREATE TABLE IF NOT EXISTS ModuleFields (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Module VARCHAR(50) NOT NULL,
    FieldDefinitionId INTEGER NOT NULL,
    IsVisible BOOLEAN DEFAULT 1,
    IsActive BOOLEAN DEFAULT 1,
    SortOrder INTEGER DEFAULT 0,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,

    FOREIGN KEY (FieldDefinitionId) REFERENCES FieldDefinitions(Id) ON DELETE CASCADE,
    UNIQUE KEY uk_module_field (Module, FieldDefinitionId),
    INDEX idx_module (Module)
);

-- =====================================================
-- 初始数据（共享基础字段）
-- =====================================================

-- 基础字段
INSERT INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, IsShared, Options, DefaultValue, IsRequired, Width, SortOrder, Status)
VALUES 
    ('name', '姓名', 'text', 'PersonService', 1, NULL, NULL, 0, 100, 1, 'Active'),
    ('phone', '联系电话', 'text', 'PersonService', 1, NULL, NULL, 0, 100, 2, 'Active'),
    ('email', '邮箱', 'text', 'PersonService', 1, NULL, NULL, 0, 150, 3, 'Active'),
    ('idCard', '身份证号', 'text', 'PersonService', 1, NULL, NULL, 0, 150, 4, 'Active'),
    ('status', '状态', 'select', 'System', 1, '["Active","Inactive"]', 'Active', 0, 80, 5, 'Active'),
    ('remark', '备注', 'textarea', 'System', 1, NULL, NULL, 0, 200, 6, 'Active'),
    ('createdAt', '创建时间', 'date', 'System', 1, NULL, NULL, 0, 120, 7, 'Active'),
    ('updatedAt', '更新时间', 'date', 'System', 1, NULL, NULL, 0, 120, 8, 'Active');

-- PersonService 共享业务字段
INSERT INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, IsShared, Options, DefaultValue, IsRequired, Width, SortOrder, Status)
VALUES 
    ('department', '部门', 'select', 'PersonService', 1, '["工程部","客服部","安保部","保洁部","行政部","财务部"]', NULL, 0, 100, 9, 'Active'),
    ('role', '职位', 'select', 'PersonService', 1, '["operator","supervisor","manager","director"]', NULL, 0, 100, 10, 'Active'),
    ('gender', '性别', 'select', 'PersonService', 1, '["男","女"]', NULL, 0, 60, 11, 'Active'),
    ('joinDate', '入职日期', 'date', 'PersonService', 1, NULL, NULL, 0, 120, 12, 'Active'),
    ('emergencyContact', '紧急联系人', 'text', 'PersonService', 1, NULL, NULL, 0, 100, 13, 'Active'),
    ('emergencyPhone', '紧急联系电话', 'text', 'PersonService', 1, NULL, NULL, 0, 120, 14, 'Active');

-- MasterDataService 共享业务字段
INSERT INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, IsShared, Options, DefaultValue, IsRequired, Width, SortOrder, Status)
VALUES 
    ('roomNo', '房号', 'text', 'MasterDataService', 1, NULL, NULL, 0, 80, 15, 'Active'),
    ('buildingId', '楼栋', 'select', 'MasterDataService', 1, '["1栋","2栋","3栋","4栋","5栋","A栋","B栋","C栋"]', NULL, 0, 80, 16, 'Active'),
    ('floor', '楼层', 'text', 'MasterDataService', 1, NULL, NULL, 0, 60, 17, 'Active'),
    ('location', '位置', 'text', 'MasterDataService', 1, NULL, NULL, 0, 150, 18, 'Active'),
    ('type', '类型', 'select', 'MasterDataService', 1, '["日常","紧急","计划","临时"]', NULL, 0, 80, 19, 'Active'),
    ('priority', '优先级', 'select', 'MasterDataService', 1, '["Low","Medium","High","Urgent"]', 'Medium', 0, 80, 20, 'Active'),
    ('category', '分类', 'select', 'MasterDataService', 1, '["设备维修","设施维护","投诉建议","咨询服务","其他"]', NULL, 0, 100, 21, 'Active'),
    ('level', '等级', 'select', 'MasterDataService', 1, '["一级","二级","三级","四级","五级"]', NULL, 0, 60, 22, 'Active'),
    ('unit', '单位', 'select', 'MasterDataService', 1, '["个","台","套","平方米","米","公斤"]', NULL, 0, 60, 23, 'Active'),
    ('cycle', '周期', 'select', 'MasterDataService', 1, '["每日","每周","每月","每季度","每年"]', NULL, 0, 80, 24, 'Active');

-- TicketService 私有字段
INSERT INTO FieldDefinitions (FieldKey, DisplayName, FieldType, Source, IsShared, Module, Options, DefaultValue, IsRequired, Width, SortOrder, Status)
VALUES 
    ('ticketNo', '工单编号', 'text', 'TicketService', 0, 'ticket', NULL, NULL, 0, 120, 25, 'Active'),
    ('title', '工单标题', 'text', 'TicketService', 0, 'ticket', NULL, NULL, 0, 200, 26, 'Active'),
    ('description', '工单描述', 'textarea', 'TicketService', 0, 'ticket', NULL, NULL, 0, 300, 27, 'Active'),
    ('createTime', '创建时间', 'date', 'System', 0, 'ticket', NULL, NULL, 0, 120, 28, 'Active'),
    ('assigneeName', '指派人', 'text', 'PersonService', 0, 'ticket', NULL, NULL, 0, 100, 29, 'Active'),
    ('handleTime', '处理时间', 'date', 'TicketService', 0, 'ticket', NULL, NULL, 0, 120, 30, 'Active'),
    ('completeTime', '完成时间', 'date', 'TicketService', 0, 'ticket', NULL, NULL, 0, 120, 31, 'Active'),
    ('contactName', '联系人', 'text', 'PersonService', 0, 'ticket', NULL, NULL, 0, 100, 32, 'Active'),
    ('contactPhone', '联系电话', 'text', 'PersonService', 0, 'ticket', NULL, NULL, 0, 120, 33, 'Active');

-- =====================================================
-- 初始模块字段关联（工单模块）
-- =====================================================

-- 获取刚才插入的 ticket 模块字段的 ID
INSERT INTO ModuleFields (Module, FieldDefinitionId, IsVisible, IsActive, SortOrder)
SELECT 'ticket', Id, 1, 1, SortOrder - 24
FROM FieldDefinitions 
WHERE FieldKey IN ('ticketNo', 'title', 'description', 'createTime', 'assigneeName', 'handleTime', 'completeTime', 'contactName', 'contactPhone', 'priority', 'status', 'remark', 'createdAt');

-- =====================================================
-- 验证查询
-- =====================================================

-- SELECT '字段定义总数: ' || COUNT(*) FROM FieldDefinitions;
-- SELECT '共享字段数: ' || COUNT(*) FROM FieldDefinitions WHERE IsShared = 1;
-- SELECT '工单模块字段数: ' || COUNT(*) FROM ModuleFields WHERE Module = 'ticket';