-- =====================================================
-- WO-Property 慢查询分析脚本
-- 版本: 1.0.0
-- 日期: 2026-05-10
-- 作者: WO-Property 数据库工程师
-- =====================================================
--
-- 用途:
--   1. 分析慢查询日志找出性能瓶颈
--   2. 识别需要优化的 SQL 语句
--   3. 生成优化建议
--
-- 使用方式:
--   - 在 MySQL 客户端执行: source slow_query_analysis.sql
--   - 或使用 pt-query-digest 工具（参见文档）
-- =====================================================

-- ========== 第一部分：识别需要优化的查询 ==========

-- 1.1 找出执行时间最长的 20 条查询
SELECT 
    query_time AS '执行时间(秒)',
    lock_time AS '锁等待(秒)',
    rows_sent AS '返回行数',
    rows_examined AS '扫描行数',
    sql_text AS 'SQL语句'
FROM 
    mysql.slow_log
WHERE 
    start_time >= DATE_SUB(NOW(), INTERVAL 7 DAY)
ORDER BY 
    query_time DESC
LIMIT 20\G

-- 1.2 找出扫描行数最多的查询（可能存在全表扫描）
SELECT 
    rows_examined AS '扫描行数',
    rows_sent AS '返回行数',
    query_time AS '执行时间(秒)',
    db AS '数据库',
    LEFT(sql_text, 300) AS 'SQL语句(截取前300字符)'
FROM 
    mysql.slow_log
WHERE 
    start_time >= DATE_SUB(NOW(), INTERVAL 7 DAY)
    AND rows_examined > 10000
ORDER BY 
    rows_examined DESC
LIMIT 10;

-- 1.3 找出返回行数少但扫描行数多的查询（典型的索引缺失案例）
SELECT 
    rows_examined AS '扫描行数',
    rows_sent AS '返回行数',
    ROUND(rows_examined / NULLIF(rows_sent, 0), 0) AS '扫描/返回比',
    query_time AS '执行时间(秒)',
    LEFT(sql_text, 400) AS 'SQL语句'
FROM 
    mysql.slow_log
WHERE 
    start_time >= DATE_SUB(NOW(), INTERVAL 7 DAY)
    AND rows_sent < 100
    AND rows_examined > 1000
ORDER BY 
    rows_examined DESC
LIMIT 10;

-- ========== 第二部分：按表和查询模式分析 ==========

-- 2.1 统计高频慢查询（相似 SQL 模板）
SELECT 
    COUNT(*) AS '执行次数',
    SUM(query_time) AS '总耗时(秒)',
    AVG(query_time) AS '平均耗时(秒)',
    MAX(query_time) AS '最慢耗时(秒)',
    SUM(rows_examined) AS '总扫描行数',
    db AS '数据库'
FROM 
    mysql.slow_log
WHERE 
    start_time >= DATE_SUB(NOW(), INTERVAL 7 DAY)
GROUP BY 
    db, LEFT(REPLACE(sql_text, '\n', ' '), 200)
ORDER BY 
    SUM(query_time) DESC
LIMIT 20;

-- 2.2 识别最常被查询的表
SELECT 
    db AS '数据库',
    COUNT(*) AS '慢查询次数',
    SUM(query_time) AS '总耗时(秒)',
    AVG(query_time) AS '平均耗时(秒)'
FROM 
    mysql.slow_log
WHERE 
    start_time >= DATE_SUB(NOW(), INTERVAL 7 DAY)
GROUP BY 
    db
ORDER BY 
    SUM(query_time) DESC;

-- ========== 第三部分：工单系统专项分析 ==========

-- 3.1 分析 tickets 表相关慢查询
SELECT 
    query_time AS '耗时(秒)',
    rows_examined AS '扫描行数',
    rows_sent AS '返回行数',
    LEFT(sql_text, 400) AS 'SQL语句'
FROM 
    mysql.slow_log
WHERE 
    start_time >= DATE_SUB(NOW(), INTERVAL 7 DAY)
    AND sql_text LIKE '%tickets%'
ORDER BY 
    query_time DESC
LIMIT 10;

-- 3.2 分析 persons 表相关慢查询（人员服务）
SELECT 
    query_time AS '耗时(秒)',
    rows_examined AS '扫描行数',
    rows_sent AS '返回行数',
    LEFT(sql_text, 400) AS 'SQL语句'
FROM 
    mysql.slow_log
WHERE 
    start_time >= DATE_SUB(NOW(), INTERVAL 7 DAY)
    AND sql_text LIKE '%persons%'
ORDER BY 
    query_time DESC
LIMIT 10;

-- 3.3 分析 ticket_process_records 表相关查询
SELECT 
    query_time AS '耗时(秒)',
    rows_examined AS '扫描行数',
    LEFT(sql_text, 400) AS 'SQL语句'
FROM 
    mysql.slow_log
WHERE 
    start_time >= DATE_SUB(NOW(), INTERVAL 7 DAY)
    AND sql_text LIKE '%ticket_process_records%'
ORDER BY 
    query_time DESC
LIMIT 5;

-- ========== 第四部分：识别常见性能问题模式 ==========

-- 4.1 深度分页问题（OFFSET 很大）
-- 这种模式通常表示需要游标分页优化
SELECT 
    query_time AS '耗时(秒)',
    rows_examined AS '扫描行数',
    rows_sent AS '返回行数',
    LEFT(sql_text, 500) AS 'SQL语句'
FROM 
    mysql.slow_log
WHERE 
    start_time >= DATE_SUB(NOW(), INTERVAL 7 DAY)
    AND sql_text REGEXP 'OFFSET [0-9]{3,}'
ORDER BY 
    query_time DESC
LIMIT 10;

-- 4.2 缺失索引的 LIKE 查询（LIKE '%xxx%' 无法使用索引）
SELECT 
    query_time AS '耗时(秒)',
    rows_examined AS '扫描行数',
    LEFT(sql_text, 500) AS 'SQL语句'
FROM 
    mysql.slow_log
WHERE 
    start_time >= DATE_SUB(NOW(), INTERVAL 7 DAY)
    AND sql_text LIKE '%LIKE%\'%'
ORDER BY 
    query_time DESC
LIMIT 10;

-- 4.3 未使用索引的全表扫描
SELECT 
    query_time AS '耗时(秒)',
    rows_examined AS '扫描行数',
    sql_text AS 'SQL语句'
FROM 
    mysql.slow_log
WHERE 
    start_time >= DATE_SUB(NOW(), INTERVAL 7 DAY)
    AND sql_text REGEXP 'Using filesort|Using temporary'
ORDER BY 
    query_time DESC
LIMIT 10;

-- ========== 第五部分：优化建议生成 ==========

-- 5.1 生成 EXPLAIN 分析（对最慢的查询）
-- 注意: 手动替换下方 SQL_TEXT 为实际慢查询

-- 建议执行以下 EXPLAIN 分析:
-- EXPLAIN SELECT * FROM tickets WHERE Status = 'Created' ORDER BY CreatedAt DESC LIMIT 20 OFFSET 10000;
-- EXPLAIN SELECT * FROM persons WHERE Department = '工程部' AND Name LIKE '张%';
-- EXPLAIN SELECT * FROM ticket_process_records WHERE TicketId = 1234 ORDER BY CreatedAt DESC;

-- 5.2 检查表碎片率（影响查询性能）
SELECT 
    TABLE_NAME AS '表名',
    ROUND(Data_length / 1024 / 1024, 2) AS '数据大小(MB)',
    ROUND(Index_length / 1024 / 1024, 2) AS '索引大小(MB)',
    ROUND((Data_length + Index_length) / 1024 / 1024, 2) AS '总大小(MB)',
    ROUND(Data_free / 1024 / 1024, 2) AS '碎片空间(MB)'
FROM 
    information_schema.TABLES
WHERE 
    table_schema = 'wo_property'
    AND Data_free > 1024 * 1024
ORDER BY 
    Data_free DESC;

-- 5.3 检查未使用索引
SELECT 
    table_name AS '表名',
    index_name AS '索引名',
    seq_in_index AS '列序号',
    column_name AS '列名',
    CARDINALITY AS '基数'
FROM 
    information_schema.STATISTICS
WHERE 
    table_schema = 'wo_property'
    AND NON_UNIQUE > 0
ORDER BY 
    table_name, index_name;

-- ========== 第六部分：性能趋势监控 ==========

-- 6.1 慢查询数量趋势（每天）
SELECT 
    DATE(start_time) AS '日期',
    COUNT(*) AS '慢查询数',
    AVG(query_time) AS '平均耗时(秒)',
    MAX(query_time) AS '最大耗时(秒)',
    SUM(rows_examined) AS '总扫描行数'
FROM 
    mysql.slow_log
WHERE 
    start_time >= DATE_SUB(NOW(), INTERVAL 30 DAY)
GROUP BY 
    DATE(start_time)
ORDER BY 
    DATE(start_time) DESC;

-- 6.2 慢查询时间分布（按小时）
SELECT 
    HOUR(start_time) AS '小时',
    COUNT(*) AS '慢查询数',
    AVG(query_time) AS '平均耗时(秒)'
FROM 
    mysql.slow_log
WHERE 
    start_time >= DATE_SUB(NOW(), INTERVAL 7 DAY)
GROUP BY 
    HOUR(start_time)
ORDER BY 
    HOUR(start_time);

-- ========== 使用 pt-query-digest 工具（推荐）==========
-- 如果服务器安装了 Percona Toolkit，推荐使用以下命令:

-- 分析最近 24 小时的慢查询
-- pt-query-digest /var/log/mysql/mysql-slow.log --since='24h'

-- 分析并输出报告到文件
-- pt-query-digest /var/log/mysql/mysql-slow.log --since='168h' > /var/log/mysql/query-analysis-report.txt

-- 只分析执行时间超过 1 秒的查询
-- pt-query-digest /var/log/mysql/mysql-slow.log --filter='$event->{query_time} >= 1'

-- 分析特定表相关的慢查询
-- pt-query-digest /var/log/mysql/mysql-slow.log --filter='$event->{sql_text} =~ /tickets/i'

-- 生成 JSON 格式报告（便于程序处理）
-- pt-query-digest /var/log/mysql/mysql-slow.log --since='24h' --format=json > /var/log/mysql/query-analysis.json