-- =====================================================
-- 字段等价映射表 (Field Equivalences)
-- 用于解决同一字段不同命名风格的映射问题
-- 如: ticket_number = TicketNumber = ticketNo
-- =====================================================

CREATE TABLE IF NOT EXISTS field_equivalences (
    id BIGINT PRIMARY KEY AUTO_INCREMENT,
    canonical_field VARCHAR(100) NOT NULL COMMENT '标准字段名（主记录）',
    equivalent_field VARCHAR(100) NOT NULL COMMENT '等价字段名',
    module VARCHAR(50) NOT NULL COMMENT '所属模块',
    field_type VARCHAR(50) DEFAULT 'string' COMMENT '字段类型',
    display_name VARCHAR(100) COMMENT '显示名称',
    status VARCHAR(20) DEFAULT 'Active' COMMENT '状态',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY uk_equiv (module, equivalent_field),
    KEY idx_canonical (canonical_field),
    KEY idx_module (module)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='字段等价映射表';

-- =====================================================
-- 初始数据：工单模块等价映射
-- =====================================================

INSERT INTO field_equivalences (canonical_field, equivalent_field, module, display_name) VALUES
-- 工单基础字段
('ticket_number', 'ticket_number', 'ticket', '工单编号'),
('ticket_number', 'TicketNumber', 'ticket', '工单编号'),
('ticket_number', 'ticketNo', 'ticket', '工单编号'),
('ticket_type', 'ticket_type', 'ticket', '工单类型'),
('ticket_type', 'TicketType', 'ticket', '工单类型'),
('title', 'title', 'ticket', '工单标题'),
('description', 'description', 'ticket', '工单描述'),
('status', 'status', 'ticket', '状态'),
('priority', 'priority', 'ticket', '优先级'),
('images', 'images', 'ticket', '图片'),
('images', 'pictures', 'ticket', '图片'),
('rating', 'rating', 'ticket', '评分'),
('source', 'source', 'ticket', '来源'),

-- 上报人信息
('reporter_name', 'reporter_name', 'ticket', '上报人姓名'),
('reporter_name', 'ReporterName', 'ticket', '上报人姓名'),
('reporter_phone', 'reporter_phone', 'ticket', '上报人电话'),
('reporter_phone', 'ReporterPhone', 'ticket', '上报人电话'),

-- 指派信息
('assignee_id', 'assignee_id', 'ticket', '指派人ID'),
('assignee_id', 'assigneeId', 'ticket', '指派人ID'),
('assignee_id', 'HandlerId', 'ticket', '处理人ID'),
('assignee_id', 'handler_id', 'ticket', '处理人ID'),
('assigned_to', 'assigned_to', 'ticket', '指派给'),
('assigned_to', 'AssignedTo', 'ticket', '指派给'),

-- 位置信息
('building_id', 'building_id', 'ticket', '楼栋ID'),
('building_id', 'buildingId', 'ticket', '楼栋ID'),
('building_id', 'BuildingId', 'ticket', '楼栋ID'),
('room_id', 'room_id', 'ticket', '房间ID'),
('room_id', 'roomId', 'ticket', '房间ID'),
('room_id', 'RoomId', 'ticket', '房间ID'),
('location', 'location', 'ticket', '位置'),
('location_detail', 'location_detail', 'ticket', '位置详情'),

-- 联系人
('contact_name', 'contact_name', 'ticket', '联系人'),
('contact_name', 'contactName', 'ticket', '联系人'),
('contact_name', 'ContactPersonName', 'ticket', '联系人'),
('contact_phone', 'contact_phone', 'ticket', '联系电话'),
('contact_phone', 'contactPhone', 'ticket', '联系电话'),

-- 时间字段
('created_at', 'created_at', 'ticket', '创建时间'),
('created_at', 'createTime', 'ticket', '创建时间'),
('created_at', 'CreatedAt', 'ticket', '创建时间'),
('updated_at', 'updated_at', 'ticket', '更新时间'),
('updated_at', 'UpdatedAt', 'ticket', '更新时间'),
('updated_at', 'UpdatedAt', 'ticket', '更新时间'),
('handle_time', 'handle_time', 'ticket', '处理时间'),
('handle_time', 'handleTime', 'ticket', '处理时间'),
('complete_time', 'complete_time', 'ticket', '完成时间'),
('complete_time', 'completeTime', 'ticket', '完成时间'),

-- 创建人/处理人
('creator_id', 'creator_id', 'ticket', '创建人ID'),
('creator_id', 'creator_id', 'ticket', '创建人ID'),

-- 工单分类
('category', 'category', 'ticket', '工单分类'),
('dispatch_status', 'dispatch_status', 'ticket', '派单状态'),
('survey_status', 'survey_status', 'ticket', '评价状态'),
('escalation_level', 'escalation_level', 'ticket', '升级级别'),

-- 租户
('tenant_id', 'tenant_id', 'ticket', '租户ID'),
('tenant_id', 'tenantId', 'ticket', '租户ID'),

-- 项目
('project_id', 'project_id', 'ticket', '项目ID'),
('project_id', 'projectId', 'ticket', '项目ID')
ON DUPLICATE KEY UPDATE display_name = VALUES(display_name);
