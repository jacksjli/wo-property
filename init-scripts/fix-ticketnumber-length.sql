-- 修复工单编号字段长度（varchar(20) → varchar(50)）
-- 支持新格式：YGHY001-WO-YYYYMM-NNNNN（22字符）
ALTER TABLE tickets MODIFY COLUMN TicketNumber VARCHAR(50) NOT NULL COMMENT '工单编号';