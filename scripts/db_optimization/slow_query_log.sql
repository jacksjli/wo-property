-- =====================================================
-- WO-Property 慢查询监控配置脚本
-- 版本: 1.0.0
-- 日期: 2026-05-10
-- 作者: WO-Property 数据库工程师
-- =====================================================
--
-- 用途:
--   1. 开启 MySQL 慢查询日志
--   2. 配置慢查询阈值（200ms）
--   3. 提供慢查询分析查询模板
--
-- 注意事项:
--   - 执行前请备份当前配置
--   - 生产环境执行前请通知相关人员
--   - 定期清理或归档慢查询日志文件
-- =====================================================

-- ===== 1. 开启慢查询日志 =====

-- 开启慢查询日志（全局会话变量，重启后失效，如需永久生效需修改 my.cnf）
SET GLOBAL slow_query_log = 'ON';

-- 指定慢查询日志文件路径
SET GLOBAL slow_query_log_file = '/var/log/mysql/mysql-slow.log';

-- ===== 2. 配置慢查询阈值 =====

-- 设置阈值为 200ms（默认 10s太长，生产环境建议 100-500ms）
SET GLOBAL long_query_time = 0.2;

-- ===== 3. 配置日志输出选项 =====

-- 记录不使用索引的查询（建议开启，帮助发现潜在性能问题）
SET GLOBAL log_queries_not_using_indexes = 'ON';

-- 记录查询时间超过 long_query_time 且扫描行数超过 min_examined_row_limit 的查询
SET GLOBAL min_examined_row_limit = 100;

-- ===== 4. 验证配置 =====
-- 执行以下语句验证配置是否生效

SELECT 
    @@slow_query_log AS '慢查询日志开启状态',
    @@slow_query_log_file AS '慢查询日志文件路径',
    @@long_query_time AS '慢查询阈值(秒)',
    @@log_queries_not_using_indexes AS '记录未使用索引查询';

-- ===== 5. 慢查询分析查询 =====

-- 5.1 查看最近的慢查询（前 10 条，按查询时间降序）
-- 说明: 替换 {start_date} 为实际的起始日期
SELECT 
    start_time AS '查询时间',
    query_time AS '查询耗时(秒)',
    lock_time AS '锁等待时间(秒)',
    rows_sent AS '返回行数',
    rows_examined AS '扫描行数',
    db AS '数据库',
    sql_text AS 'SQL语句'
FROM 
    mysql.slow_log
WHERE 
    start_time >= DATE_SUB(NOW(), INTERVAL 7 DAY)
ORDER BY 
    query_time DESC
LIMIT 10;

-- 5.2 统计慢查询高频表（按表分组统计）
SELECT 
    LEFT(REPLACE(sql_text, '\n', ' '), 200) AS 'SQL模板',
    COUNT(*) AS '出现次数',
    SUM(query_time) AS '总耗时(秒)',
    AVG(query_time) AS '平均耗时(秒)',
    MAX(query_time) AS '最大耗时(秒)',
    SUM(rows_examined) AS '总扫描行数'
FROM 
    mysql.slow_log
WHERE 
    start_time >= DATE_SUB(NOW(), INTERVAL 7 DAY)
GROUP BY 
    LEFT(REPLACE(sql_text, '\n', ' '), 200)
ORDER BY 
    COUNT(*) DESC
LIMIT 20;

-- 5.3 分析慢查询的表访问模式
SELECT 
    db AS '数据库',
    COUNT(DISTINCT sql_text) AS '不同SQL数',
    COUNT(*) AS '总执行次数',
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

-- ===== 6. 使用 pt-query-digest 分析慢查询（如果已安装 Percona Toolkit）=====

-- 6.1 分析最近 24 小时的慢查询
-- pt-query-digest /var/log/mysql/mysql-slow.log --since='24h'

-- 6.2 分析最近一周的慢查询，并输出到文件
-- pt-query-digest /var/log/mysql/mysql-slow.log --since='168h' > /var/log/mysql/slow-query-report.txt

-- 6.3 只分析执行时间超过 1 秒的查询
-- pt-query-digest /var/log/mysql/mysql-slow.log --filter='$event->{query_time} >= 1'

-- 6.4 分析特定表的慢查询
-- pt-query-digest /var/log/mysql/mysql-slow.log --filter='$event->{sql_text} =~ /tickets/i'

-- ===== 7. 自动化慢查询监控（建议配置 cron job）=====

-- 建议在数据库服务器上配置定时任务，每天生成慢查询报告
-- 示例 cron 配置（每天凌晨 2 点执行）:
-- 0 2 * * * pt-query-digest /var/log/mysql/mysql-slow.log --since='24h' > /var/log/mysql/slow-report-$(date +\%Y\%m\%d).txt

-- ===== 8. 性能基线监控 =====
-- 建议每周检查一次，建立性能基线

-- 检查平均查询时间趋势
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

-- ===== 9. 配置持久化（需要重启 MySQL 服务）=====
-- 以下配置需要添加到 MySQL 配置文件 /etc/mysql/my.cnf 或 /etc/mysql/mysql.conf.d/mysqld.cnf

/*
[mysqld]
# 慢查询日志配置
slow_query_log = 1
slow_query_log_file = /var/log/mysql/mysql-slow.log
long_query_time = 0.2
log_queries_not_using_indexes = 1
min_examined_row_limit = 100

# 注意: 修改配置文件后需要执行以下命令重启 MySQL
# systemctl restart mysql
# 或
# service mysql restart
*/

-- ===== 10. 清理旧的慢查询日志（可选）=====
-- 当慢查询日志文件过大时，可以手动清理
-- 注意: 这只是清空文件内容，不会禁用慢查询日志

-- 查看慢查询日志大小
-- SELECT file_name, table_size FROM mysql.general_log JOIN mysql.tablespace_usage USING (table_id);

-- 清空调试日志（如果使用的是 general_log 而非 slow_log）
-- SET GLOBAL general_log = 'OFF';
-- TRUNCATE TABLE mysql.general_log;
-- SET GLOBAL general_log = 'ON';

-- ===== 注意事项 =====

-- 1. 慢查询日志会消耗额外的磁盘 I/O，建议监控日志文件大小
-- 2. 在高并发场景下，开启 log_queries_not_using_indexes 可能产生大量日志
-- 3. 建议配合 pt-query-digest 使用，自动聚合相似查询
-- 4. 定期归档和压缩旧的慢查询日志
-- 5. 生产环境执行前请评估对性能的影响（通常很小）