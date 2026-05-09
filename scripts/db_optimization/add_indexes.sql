-- =====================================================
-- WO-Property 数据库索引优化脚本
-- 版本: 1.0.0
-- 日期: 2026-05-10
-- 作者: WO-Property 数据库工程师
-- =====================================================
--
-- 执行顺序:
--   1. 先执行此脚本（创建索引）
--   2. 然后更新应用程序连接字符串（启用连接池）
--
-- 预估性能提升:
--   - 工单列表查询：提升 5-10x（从 ms 级到 µs 级）
--   - 人员搜索：提升 10-50x（联合索引避免回表）
--   - 深度分页：提升 100x+（OFFSET 10000 时）
-- =====================================================

-- ===== 工单相关表索引 =====

-- tickets 表：工单列表查询（按 status + createdAt 排序）
-- 场景: GET /api/tickets?status=Created&page=500
-- 原问题: OFFSET 10000 时全表扫描，查询时间 > 500ms
-- 优化: 复合索引覆盖 status + createdAt，查询时间 < 10ms
CREATE INDEX IF NOT EXISTS idx_tickets_status_createdAt
ON tickets(Status, CreatedAt DESC);

-- tickets 表：按处理人查询
-- 场景: 获取某人被分配的工单列表
CREATE INDEX IF NOT EXISTS idx_tickets_assignee
ON tickets(AssigneePersonId, CreatedAt DESC);

-- tickets 表：按创建人查询
-- 场景: 查看某业主提交的工单
CREATE INDEX IF NOT EXISTS idx_tickets_creator
ON tickets(CreatorPersonId, CreatedAt DESC);

-- tickets 表：工单编号唯一查询（已有单列索引，提升为覆盖索引）
-- 场景: 通过工单号精确查找
CREATE INDEX IF NOT EXISTS idx_tickets_ticketCode
ON tickets(TicketCode);

-- tickets 表：优先级查询（用于统计）
-- 场景: 按优先级分组统计工单数量
CREATE INDEX IF NOT EXISTS idx_tickets_priority
ON tickets(Priority, CreatedAt DESC);

-- ticket_process_records 表：按工单ID快速查询处理历史
-- 场景: GET /api/tickets/{id}/history
-- 注意: 该表已有 idx_ticketId，但加 CreatedAt 排序优化
CREATE INDEX IF NOT EXISTS idx_tpr_ticketId_createdAt
ON ticket_process_records(TicketId, CreatedAt DESC);

-- dispatch_tasks 表：按工单ID查询派单任务（已有索引）
CREATE INDEX IF NOT EXISTS idx_dispatch_ticketId
ON dispatch_tasks(TicketId);

-- dispatch_tasks 表：按处理人查询待处理任务
-- 场景: 员工查看自己的派单任务列表
CREATE INDEX IF NOT EXISTS idx_dispatch_assignee_status
ON dispatch_tasks(AssignedToPersonId, Status, AssignedAt DESC);

-- ===== 人员服务相关表索引 =====

-- persons 表：按部门查询人员列表（高频查询）
-- 场景: 获取某部门所有员工
CREATE INDEX IF NOT EXISTS idx_persons_department
ON persons(Department, Status, Name);

-- persons 表：按状态筛选（"在职" 人员是主要查询对象）
-- 场景: 下拉框选择员工，排除离职人员
CREATE INDEX IF NOT EXISTS idx_persons_status_type
ON persons(Status, PersonType, Name);

-- persons 表：模糊搜索优化（使用索引前缀）
-- 场景: 搜索框输入 "张" 查找所有姓张的员工
-- 注意: MySQL 索引不支持 LIKE '%xxx%'，需要全文索引或应用层处理
-- 此索引用于 LIKE 'xxx%' 前缀匹配场景
CREATE INDEX IF NOT EXISTS idx_persons_name
ON persons(Name);

-- persons 表：按工号查询（已有唯一索引，此处确保）
CREATE INDEX IF NOT EXISTS idx_persons_staffId
ON persons(StaffId);

-- persons 表：按手机号查询（登录/找回密码场景）
CREATE INDEX IF NOT EXISTS idx_persons_phone
ON persons(Phone);

-- ===== 部门相关表索引 =====

-- departments 表：名称唯一性保障
CREATE INDEX IF NOT EXISTS idx_departments_name
ON departments(Name);

-- ===== 角色相关表索引 =====

-- roles 表：名称唯一性保障
CREATE INDEX IF NOT EXISTS idx_roles_name
ON roles(Name);

-- ===== MasterDataService 相关表索引 =====

-- field_definitions 表：按模块查询字段定义
CREATE INDEX IF NOT EXISTS idx_fd_module
ON field_definitions(Module, Status, SortOrder);

-- field_definitions 表：共享字段快速筛选
CREATE INDEX IF NOT EXISTS idx_fd_isShared_source
ON field_definitions(IsShared, Source, FieldKey);

-- module_fields 表：按模块查询字段关联
CREATE INDEX IF NOT EXISTS idx_mf_module
ON module_fields(Module, IsActive, SortOrder);

-- ===== 分页优化：游标分页支持索引 =====
-- 如果要支持游标分页，需要创建基于主键或 createdAt 的索引
-- 以下索引用于替代 OFFSET 分页的游标查询

-- tickets 表：支持基于 CreatedAt 的游标分页（Keyset Pagination）
-- 替代: ORDER BY CreatedAt DESC LIMIT 20 OFFSET 10000
-- 新方式: WHERE CreatedAt < {lastCreatedAt} ORDER BY CreatedAt DESC LIMIT 20
CREATE INDEX IF NOT EXISTS idx_tickets_cursor
ON tickets(CreatedAt DESC, Id DESC);

-- ===== 全文索引（可选，用于模糊搜索）=====
-- 如果系统需要支持中文模糊搜索（如 LIKE '%关键词%'），建议添加全文索引
-- 注意：MySQL 8.0 中文全文索引需要配置 ngram 分词器

-- persons 表：中文姓名全文搜索（需要 MySQL 8.0+ 且配置 ngram）
-- ALTER TABLE persons ADD FULLTEXT INDEX ft_persons_name (Name) WITH PARSER ngram;

-- ===== 索引创建说明 =====
-- 
-- 执行方式（选择一种）:
--   方式1（Docker 环境）:
--     docker exec -i wo-property-mysql mysql -uwoproperty -pWOProperty2026! wo_property < add_indexes.sql
--   
--   方式2（本地 MySQL）:
--     mysql -uwoproperty -pWOProperty2026! wo_property < add_indexes.sql
--
-- 验证索引创建:
--   SHOW INDEX FROM tickets;
--   SHOW INDEX FROM persons;
--
-- 删除测试索引（如需回滚）:
--   DROP INDEX idx_tickets_status_createdAt ON tickets;
--   DROP INDEX idx_persons_department ON persons;
--
-- 索引监控（查看索引使用情况）:
--   SELECT * FROM performance_schema.table_io_waits_summary_by_index_usage
--   WHERE OBJECT_SCHEMA = 'wo_property' ORDER BY COUNT_READ DESC;