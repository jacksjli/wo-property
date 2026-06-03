-- =============================================
-- 派单模块数据库表创建脚本
-- 创建时间：2026-05-23
-- 数据库：wo_property
-- =============================================

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ----------------------------
-- 1. dispatch_records（派单记录表）
-- ----------------------------
DROP TABLE IF EXISTS `dispatch_records`;
CREATE TABLE `dispatch_records` (
  `id` bigint NOT NULL AUTO_INCREMENT COMMENT '派单记录ID',
  `ticket_id` bigint NOT NULL COMMENT '工单ID',
  `ticket_code` varchar(50) NOT NULL COMMENT '工单编号',
  `dispatch_time` datetime NOT NULL COMMENT '派单时间',
  `from_person_id` int NOT NULL DEFAULT '0' COMMENT '派单人(0=系统)',
  `from_person_name` varchar(50) DEFAULT NULL COMMENT '派单人姓名',
  `to_person_id` int NOT NULL COMMENT '维修人员ID',
  `to_person_name` varchar(50) DEFAULT NULL COMMENT '维修人员姓名',
  `status` varchar(20) NOT NULL DEFAULT 'Pending' COMMENT '状态: Pending/WReceived/WProcessing/Completed/Transferred',
  `source` varchar(20) NOT NULL DEFAULT 'Auto' COMMENT '来源: Auto/Manual/Transfer',
  `workflow_instance_id` varchar(50) DEFAULT NULL COMMENT '工作流实例ID',
  `completed_at` datetime DEFAULT NULL COMMENT '完工时间',
  `confirmed_at` datetime DEFAULT NULL COMMENT '确认时间',
  `confirmed_by` int DEFAULT NULL COMMENT '确认人ID',
  `confirmed_by_name` varchar(50) DEFAULT NULL COMMENT '确认人姓名',
  `rating_id` bigint DEFAULT NULL COMMENT '满意度评价ID',
  `tenant_code` varchar(50) NOT NULL COMMENT '租户编码',
  `project_id` int NOT NULL COMMENT '项目ID',
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `updated_at` datetime DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
  PRIMARY KEY (`id`),
  KEY `idx_ticket_id` (`ticket_id`),
  KEY `idx_to_person` (`to_person_id`),
  KEY `idx_status` (`status`),
  KEY `idx_tenant_project` (`tenant_code`,`project_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='派单记录表';

-- ----------------------------
-- 2. transfer_requests（转单申请记录表）
-- ----------------------------
DROP TABLE IF EXISTS `transfer_requests`;
CREATE TABLE `transfer_requests` (
  `id` bigint NOT NULL AUTO_INCREMENT COMMENT '转单申请ID',
  `dispatch_record_id` bigint NOT NULL COMMENT '原始派单记录ID',
  `ticket_id` bigint NOT NULL COMMENT '工单ID',
  `ticket_code` varchar(50) NOT NULL COMMENT '工单编号',
  `from_person_id` int NOT NULL COMMENT '申请人ID',
  `from_person_name` varchar(50) DEFAULT NULL COMMENT '申请人姓名',
  `to_person_id` int NOT NULL COMMENT '目标人员ID',
  `to_person_name` varchar(50) DEFAULT NULL COMMENT '目标人员姓名',
  `reason` varchar(500) NOT NULL COMMENT '转单原因',
  `status` varchar(20) NOT NULL DEFAULT 'Pending' COMMENT '状态: Pending/Approved/Rejected',
  `approved_by` int DEFAULT NULL COMMENT '审批人ID',
  `approved_by_name` varchar(50) DEFAULT NULL COMMENT '审批人姓名',
  `approved_at` datetime DEFAULT NULL COMMENT '审批时间',
  `approved_reason` varchar(500) DEFAULT NULL COMMENT '审批备注',
  `transfer_dispatch_id` bigint DEFAULT NULL COMMENT '转单后新派单记录ID',
  `tenant_code` varchar(50) NOT NULL COMMENT '租户编码',
  `project_id` int NOT NULL COMMENT '项目ID',
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `updated_at` datetime DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
  PRIMARY KEY (`id`),
  KEY `idx_ticket_id` (`ticket_id`),
  KEY `idx_from_person` (`from_person_id`),
  KEY `idx_status` (`status`),
  KEY `idx_tenant_project` (`tenant_code`,`project_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='转单申请记录表';

-- ----------------------------
-- 3. dispatch_rules（派单规则表）
-- ----------------------------
DROP TABLE IF EXISTS `dispatch_rules`;
CREATE TABLE `dispatch_rules` (
  `id` bigint NOT NULL AUTO_INCREMENT COMMENT '规则ID',
  `rule_name` varchar(100) NOT NULL COMMENT '规则名称',
  `priority` int NOT NULL DEFAULT '0' COMMENT '优先级(数字越大越优先)',
  `ticket_type_id` int DEFAULT NULL COMMENT '工单类型ID(NULL表示所有类型)',
  `ticket_type_name` varchar(50) DEFAULT NULL COMMENT '工单类型名称',
  `area_id` int DEFAULT NULL COMMENT '区域ID(NULL表示所有区域)',
  `area_name` varchar(50) DEFAULT NULL COMMENT '区域名称',
  `person_id` int DEFAULT NULL COMMENT '指定人员ID(NULL表示自动匹配)',
  `person_name` varchar(50) DEFAULT NULL COMMENT '指定人员姓名',
  `balance_strategy` varchar(20) NOT NULL DEFAULT 'LeastWorkload' COMMENT '负载均衡策略: LeastWorkload/RoundRobin/SkillScore',
  `is_active` tinyint NOT NULL DEFAULT '1' COMMENT '是否启用: 0=禁用, 1=启用',
  `tenant_code` varchar(50) NOT NULL COMMENT '租户编码',
  `project_id` int NOT NULL COMMENT '项目ID',
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `updated_at` datetime DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
  PRIMARY KEY (`id`),
  KEY `idx_tenant_project` (`tenant_code`,`project_id`),
  KEY `idx_priority` (`priority`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='派单规则表';

-- ----------------------------
-- 4. person_workload（人员负载统计表）
-- ----------------------------
DROP TABLE IF EXISTS `person_workload`;
CREATE TABLE `person_workload` (
  `person_id` int NOT NULL COMMENT '人员ID',
  `person_name` varchar(50) DEFAULT NULL COMMENT '人员姓名',
  `active_ticket_count` int NOT NULL DEFAULT '0' COMMENT '当前待处理工单数',
  `total_dispatched` int NOT NULL DEFAULT '0' COMMENT '累计派单数',
  `total_completed` int NOT NULL DEFAULT '0' COMMENT '累计完成数',
  `last_dispatch_time` datetime DEFAULT NULL COMMENT '最后派单时间',
  `tenant_code` varchar(50) NOT NULL COMMENT '租户编码',
  `project_id` int NOT NULL COMMENT '项目ID',
  `updated_at` datetime DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
  PRIMARY KEY (`person_id`),
  KEY `idx_tenant_project` (`tenant_code`,`project_id`),
  KEY `idx_active_count` (`active_ticket_count`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='人员负载统计表';

-- ----------------------------
-- 5. satisfaction_ratings（满意度评价表）
-- ----------------------------
DROP TABLE IF EXISTS `satisfaction_ratings`;
CREATE TABLE `satisfaction_ratings` (
  `id` bigint NOT NULL AUTO_INCREMENT COMMENT '评价ID',
  `ticket_id` bigint NOT NULL COMMENT '工单ID',
  `ticket_code` varchar(50) NOT NULL COMMENT '工单编号',
  `dispatch_record_id` bigint NOT NULL COMMENT '派单记录ID',
  `rater_id` int NOT NULL COMMENT '评价人ID(工单创建人)',
  `rater_name` varchar(50) DEFAULT NULL COMMENT '评价人姓名',
  `ratee_id` int NOT NULL COMMENT '被评价人ID(维修人员)',
  `ratee_name` varchar(50) DEFAULT NULL COMMENT '被评价人姓名',
  `quality_score` tinyint NOT NULL DEFAULT '5' COMMENT '维修质量(1-5星)',
  `attitude_score` tinyint NOT NULL DEFAULT '5' COMMENT '服务态度(1-5星)',
  `timeliness_score` tinyint NOT NULL DEFAULT '5' COMMENT '到达时效(1-5星)',
  `overall_score` tinyint NOT NULL DEFAULT '5' COMMENT '总体评分(1-5星)',
  `comment` varchar(500) DEFAULT NULL COMMENT '评价文字',
  `images` varchar(1000) DEFAULT NULL COMMENT '评价图片(JSON数组)',
  `rated_at` datetime NOT NULL COMMENT '评价时间',
  `is_auto_rated` tinyint NOT NULL DEFAULT '0' COMMENT '是否自动评价: 0=手动, 1=自动',
  `tenant_code` varchar(50) NOT NULL COMMENT '租户编码',
  `project_id` int NOT NULL COMMENT '项目ID',
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `updated_at` datetime DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
  PRIMARY KEY (`id`),
  KEY `idx_ticket_id` (`ticket_id`),
  KEY `idx_rater_id` (`rater_id`),
  KEY `idx_ratee_id` (`ratee_id`),
  KEY `idx_overall_score` (`overall_score`),
  KEY `idx_tenant_project` (`tenant_code`,`project_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='满意度评价表';

-- ----------------------------
-- 6. timeout_alerts（超时告警记录表）
-- ----------------------------
DROP TABLE IF EXISTS `timeout_alerts`;
CREATE TABLE `timeout_alerts` (
  `id` bigint NOT NULL AUTO_INCREMENT COMMENT '告警ID',
  `ticket_id` bigint NOT NULL COMMENT '工单ID',
  `ticket_code` varchar(50) NOT NULL COMMENT '工单编号',
  `dispatch_record_id` bigint DEFAULT NULL COMMENT '派单记录ID',
  `alert_type` varchar(30) NOT NULL COMMENT '告警类型: PendingTimeout/ProcessingTimeout/CompletedTimeout',
  `expected_time` datetime NOT NULL COMMENT '期望完成时间',
  `actual_time` datetime DEFAULT NULL COMMENT '实际时间',
  `timeout_minutes` int NOT NULL COMMENT '超时分钟数',
  `level` int NOT NULL DEFAULT '1' COMMENT '告警级别: 1-4',
  `notify_target_id` int NOT NULL COMMENT '通知对象ID',
  `notify_target_name` varchar(50) DEFAULT NULL COMMENT '通知对象姓名',
  `notification_id` bigint DEFAULT NULL COMMENT '通知记录ID',
  `status` varchar(20) NOT NULL DEFAULT 'Pending' COMMENT '状态: Pending/Sent/Read/Processed',
  `sent_at` datetime DEFAULT NULL COMMENT '发送时间',
  `processed_at` datetime DEFAULT NULL COMMENT '处理时间',
  `tenant_code` varchar(50) NOT NULL COMMENT '租户编码',
  `project_id` int NOT NULL COMMENT '项目ID',
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  PRIMARY KEY (`id`),
  KEY `idx_ticket_id` (`ticket_id`),
  KEY `idx_alert_type` (`alert_type`),
  KEY `idx_status` (`status`),
  KEY `idx_level` (`level`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='超时告警记录表';

SET FOREIGN_KEY_CHECKS = 1;

-- =============================================
-- 验证查询
-- =============================================
-- SHOW TABLES LIKE 'dispatch_records';
-- SHOW TABLES LIKE 'transfer_requests';
-- SHOW TABLES LIKE 'dispatch_rules';
-- SHOW TABLES LIKE 'person_workload';
-- SHOW TABLES LIKE 'satisfaction_ratings';
-- SHOW TABLES LIKE 'timeout_alerts';