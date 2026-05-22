using MySqlConnector;
using System.Text;
using System.Text.RegularExpressions;

namespace WO.Property.CenterService.Services;

/// <summary>
/// 项目数据库服务 - 负责创建新项目的数据库和表结构
/// 支持 Schema 版本控制和增量迁移
/// </summary>
public class ProjectDatabaseService
{
    private readonly string _connectionString;
    private readonly ILogger<ProjectDatabaseService> _logger;

    // 当前标准 Schema 版本
    private const string CURRENT_SCHEMA_VERSION = "v1.0";

    public ProjectDatabaseService(IConfiguration configuration, ILogger<ProjectDatabaseService> logger)
    {
        _connectionString = $"Server={configuration["Database:Server"] ?? "127.0.0.1"};Port={configuration["Database:Port"] ?? "3306"};User={configuration["Database:Username"] ?? "root"};Password={configuration["Database:Password"] ?? ""};";
        _logger = logger;
    }

    /// <summary>
    /// 创建新项目数据库并初始化表结构
    /// </summary>
    public async Task<(bool Success, string Message)> CreateProjectDatabaseAsync(string projectCode)
    {
        var dbName = $"project_{projectCode.ToLower()}";

        _logger.LogInformation("开始创建项目数据库: {DatabaseName}", dbName);

        try
        {
            // 1. 创建数据库
            await CreateDatabaseAsync(dbName);

            // 2. 执行初始化脚本
            await ExecuteInitScriptAsync(dbName);

            // 3. 验证表是否创建成功
            var tablesExist = await VerifyTablesAsync(dbName);

            if (tablesExist)
            {
                // 4. 记录 Schema 版本
                await RecordSchemaVersionAsync(dbName, CURRENT_SCHEMA_VERSION);

                _logger.LogInformation("项目数据库 {DatabaseName} 创建成功", dbName);
                return (true, $"项目数据库 {dbName} 创建成功");
            }
            else
            {
                return (false, "表结构创建不完整");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建项目数据库失败: {DatabaseName}", dbName);
            return (false, $"创建失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 升级现有项目数据库到最新 Schema
    /// </summary>
    public async Task<(bool Success, string Message, List<string> MigratedTables)> MigrateDatabaseAsync(string projectCode)
    {
        var dbName = $"project_{projectCode.ToLower()}";
        var migratedTables = new List<string>();

        _logger.LogInformation("开始检查并升级项目数据库: {DatabaseName}", dbName);

        try
        {
            // 检查数据库是否存在
            if (!await DatabaseExistsAsync(dbName))
            {
                return (false, $"数据库 {dbName} 不存在", migratedTables);
            }

            // 获取当前数据库的 Schema 版本
            var currentVersion = await GetCurrentSchemaVersionAsync(dbName);

            if (currentVersion == CURRENT_SCHEMA_VERSION)
            {
                _logger.LogInformation("数据库 {DatabaseName} 已是最新版本 {Version}", dbName, currentVersion);
                return (true, $"数据库已是最新版本 {currentVersion}", migratedTables);
            }

            // 获取标准 Schema 定义
            var standardTables = await GetStandardTableDefinitionsAsync();

            // 获取当前数据库的表
            var existingTables = await GetExistingTablesAsync(dbName);

            // 对比并补齐缺失的表和列
            await using var conn = new MySqlConnection(_connectionString + $"Database={dbName}");
            await conn.OpenAsync();

            foreach (var (tableName, columns) in standardTables)
            {
                if (existingTables.Contains(tableName))
                {
                    // 表存在，检查缺失的列
                    var existingColumns = await GetExistingColumnsAsync(conn, tableName);
                    var missingColumns = columns.Where(c => !existingColumns.ContainsKey(c.Key)).ToList();

                    if (missingColumns.Count > 0)
                    {
                        _logger.LogInformation("表 {Table} 缺少 {Count} 列，开始补齐", tableName, missingColumns.Count);

                        foreach (KeyValuePair<string, string> col in missingColumns)
                        {
                            try
                            {
                                var alterSql = $"ALTER TABLE `{tableName}` ADD COLUMN `{col.Key}` {col.Value}";
                                await using var alterCmd = new MySqlCommand(alterSql, conn);
                                await alterCmd.ExecuteNonQueryAsync();
                                _logger.LogInformation("  ✓ 添加列 {Table}.{Column}", tableName, col.Key);
                            }
                            catch (MySqlException ex)
                            {
                                _logger.LogWarning("添加列失败: {Table}.{Column} - {Error}", tableName, col.Key, ex.Message);
                            }
                        }

                        migratedTables.Add($"{tableName} (补列: {missingColumns.Count})");
                    }
                }
                else
                {
                    // 表不存在，创建表
                    _logger.LogInformation("表 {Table} 不存在，需要从初始化脚本执行", tableName);
                    // 注：完整建表需要完整的 CREATE TABLE 语句，这里仅记录
                    migratedTables.Add($"{tableName} (缺表，需重建)");
                }
            }

            // 记录新版本
            await RecordSchemaVersionAsync(dbName, CURRENT_SCHEMA_VERSION);

            var summary = migratedTables.Count > 0
                ? $"已升级，迁移了 {migratedTables.Count} 个对象"
                : "无需迁移";

            _logger.LogInformation("数据库升级完成: {Summary}", summary);
            return (true, $"{summary}，版本: {CURRENT_SCHEMA_VERSION}", migratedTables);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "升级项目数据库失败: {DatabaseName}", dbName);
            return (false, $"升级失败: {ex.Message}", migratedTables);
        }
    }

    private async Task RecordSchemaVersionAsync(string dbName, string version)
    {
        await using var conn = new MySqlConnection(_connectionString + $"Database={dbName}");
        await conn.OpenAsync();

        // 先确保 _schema_version 表存在
        var createTableSql = @"
        CREATE TABLE IF NOT EXISTS `_schema_version` (
          `version` varchar(20) NOT NULL,
          `applied_at` datetime DEFAULT CURRENT_TIMESTAMP,
          `description` varchar(200) DEFAULT NULL,
          PRIMARY KEY (`version`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci";
        await using var createCmd = new MySqlCommand(createTableSql, conn);
        await createCmd.ExecuteNonQueryAsync();

        var sql = $"INSERT INTO `_schema_version` (version, description) VALUES ('{version}', 'Migrated to {version}')";
        await using var cmd = new MySqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();

        _logger.LogInformation("记录 Schema 版本: {Version}", version);
    }

    private async Task<string?> GetCurrentSchemaVersionAsync(string dbName)
    {
        try
        {
            await using var conn = new MySqlConnection(_connectionString + $"Database={dbName}");
            await conn.OpenAsync();

            // 检查 _schema_version 表是否存在
            var checkSql = "SHOW TABLES LIKE '_schema_version'";
            await using var checkCmd = new MySqlCommand(checkSql, conn);
            await using var reader = await checkCmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null; // 没有版本记录，说明是旧数据库
            }
            reader.Close();

            // 获取最新版本
            var versionSql = "SELECT version FROM `_schema_version` ORDER BY applied_at DESC LIMIT 1";
            await using var versionCmd = new MySqlCommand(versionSql, conn);
            var result = await versionCmd.ExecuteScalarAsync();
            return result?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private async Task<bool> DatabaseExistsAsync(string dbName)
    {
        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        var sql = $"SHOW DATABASES LIKE '{dbName}'";
        await using var cmd = new MySqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync();
    }

    private async Task<HashSet<string>> GetExistingTablesAsync(string dbName)
    {
        await using var conn = new MySqlConnection(_connectionString + $"Database={dbName}");
        await conn.OpenAsync();

        var tables = new HashSet<string>();
        var sql = "SHOW TABLES";
        await using var cmd = new MySqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }
        return tables;
    }

    private async Task<Dictionary<string, string>> GetExistingColumnsAsync(MySqlConnection conn, string tableName)
    {
        var columns = new Dictionary<string, string>();
        var sql = $"SHOW COLUMNS FROM `{tableName}`";
        await using var cmd = new MySqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columns[reader.GetString(0)] = reader.GetString(1); // name -> type
        }
        return columns;
    }

    /// <summary>
    /// 从 canonical-schema.sql 解析标准表定义
    /// 返回：表名 -> (列名 -> 列定义)
    /// </summary>
    private async Task<Dictionary<string, Dictionary<string, string>>> GetStandardTableDefinitionsAsync()
    {
        var scriptPath = "/Users/mac/Projects/WO-Property-Management/init-scripts/canonical-schema.sql";
        var script = await File.ReadAllTextAsync(scriptPath);

        var tables = new Dictionary<string, Dictionary<string, string>>();
        var currentTable = "";
        var currentColumns = new Dictionary<string, string>();

        var lines = script.Split('\n');
        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // 匹配 CREATE TABLE
            var createMatch = Regex.Match(trimmed, @"CREATE TABLE IF NOT EXISTS `([^`]+)`", RegexOptions.IgnoreCase);
            if (createMatch.Success)
            {
                // 保存上一个表
                if (!string.IsNullOrEmpty(currentTable) && currentColumns.Count > 0)
                {
                    tables[currentTable] = currentColumns;
                }

                currentTable = createMatch.Groups[1].Value;
                currentColumns = new Dictionary<string, string>();
                continue;
            }

            // 在表定义内，匹配列定义
            if (!string.IsNullOrEmpty(currentTable) && trimmed.StartsWith("`") && trimmed.Contains("` "))
            {
                var colMatch = Regex.Match(trimmed, @"`([^`]+)`\s+([A-Za-z0-9(),.'_]+)");
                if (colMatch.Success)
                {
                    var colName = colMatch.Groups[1].Value;
                    var colDef = colMatch.Groups[2].Value;
                    currentColumns[colName] = colDef;
                }
            }

            // 表定义结束
            if (currentTable != "" && trimmed == ");")
            {
                if (currentColumns.Count > 0)
                {
                    tables[currentTable] = currentColumns;
                }
                currentTable = "";
                currentColumns = new Dictionary<string, string>();
            }
        }

        // 保存最后一个表
        if (!string.IsNullOrEmpty(currentTable) && currentColumns.Count > 0)
        {
            tables[currentTable] = currentColumns;
        }

        return tables;
    }

    private async Task CreateDatabaseAsync(string dbName)
    {
        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        var sql = $"CREATE DATABASE IF NOT EXISTS `{dbName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci";
        await using var cmd = new MySqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();

        _logger.LogInformation("数据库 {DatabaseName} 创建完成", dbName);
    }

    private async Task ExecuteInitScriptAsync(string dbName)
    {
        var scriptPath = "/Users/mac/Projects/WO-Property-Management/init-scripts/canonical-schema.sql";

        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException($"找不到初始化脚本: {scriptPath}");
        }

        var script = await File.ReadAllTextAsync(scriptPath);
        _logger.LogInformation("读取初始化脚本: {ScriptPath}", scriptPath);

        // 分割 SQL 语句并执行
        var statements = SplitSqlStatements(script);

        // 先 USE 数据库
        await using var initConn = new MySqlConnection(_connectionString + $"Database={dbName}");
        await initConn.OpenAsync();

        int executedCount = 0;
        int errorCount = 0;

        foreach (var statement in statements)
        {
            if (string.IsNullOrWhiteSpace(statement)) continue;

            try
            {
                await using var cmd = new MySqlCommand(statement, initConn);
                await cmd.ExecuteNonQueryAsync();
                executedCount++;
            }
            catch (MySqlException ex) when (ex.Number == 1065) // Empty query
            {
                // 忽略空查询
            }
            catch (MySqlException ex)
            {
                errorCount++;
                _logger.LogWarning("SQL 执行警告: {Message} | Statement: {Statement}", ex.Message, statement.Substring(0, Math.Min(50, statement.Length)));
            }
        }

        _logger.LogInformation("执行了 {Count} 条 SQL 语句 ({Errors} 个警告)", executedCount, errorCount);
    }

    private async Task<bool> VerifyTablesAsync(string dbName)
    {
        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        // 切换到指定数据库
        await using (var useCmd = new MySqlCommand($"USE `{dbName}`", conn))
        {
            await useCmd.ExecuteNonQueryAsync();
        }

        var tables = new[] { "persons", "Tickets", "Areas", "Buildings", "Rooms", "Departments", "Devices" };

        foreach (var table in tables)
        {
            var sql = $"SHOW TABLES LIKE '{table}'";
            await using var cmd = new MySqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                _logger.LogWarning("表 {Table} 未创建", table);
                return false;
            }
            reader.Close();
        }

        return true;
    }

    private List<string> SplitSqlStatements(string sql)
    {
        var statements = new List<string>();
        var current = new StringBuilder();
        var inMultiLineComment = false;
        var inSingleLineComment = false;
        var inString = false;
        var stringChar = '\'';

        for (int i = 0; i < sql.Length; i++)
        {
            var c = sql[i];
            var nextC = i + 1 < sql.Length ? sql[i + 1] : '\0';

            // 处理单行注释
            if (!inString && !inMultiLineComment && c == '-' && nextC == '-')
            {
                inSingleLineComment = true;
                continue;
            }

            // 处理多行注释
            if (!inString && !inSingleLineComment && c == '/' && nextC == '*')
            {
                inMultiLineComment = true;
                i++;
                continue;
            }
            if (!inString && !inSingleLineComment && c == '*' && nextC == '/')
            {
                inMultiLineComment = false;
                i++;
                continue;
            }

            // 跳过注释内容
            if (inSingleLineComment)
            {
                if (c == '\n')
                    inSingleLineComment = false;
                continue;
            }
            if (inMultiLineComment)
                continue;

            // 处理字符串
            if (c == stringChar)
            {
                inString = !inString;
                current.Append(c);
            }
            else if (c == ';' && !inString)
            {
                var stmt = current.ToString().Trim();
                if (!string.IsNullOrEmpty(stmt))
                    statements.Add(stmt);
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        // 处理最后一条语句
        var lastStmt = current.ToString().Trim();
        if (!string.IsNullOrEmpty(lastStmt))
            statements.Add(lastStmt);

        return statements;
    }

    /// <summary>
    /// 删除项目数据库
    /// </summary>
    public async Task<(bool Success, string Message)> DeleteProjectDatabaseAsync(string projectCode)
    {
        var dbName = $"project_{projectCode.ToLower()}";

        _logger.LogInformation("开始删除项目数据库: {DatabaseName}", dbName);

        try
        {
            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            // 删除数据库
            var sql = $"DROP DATABASE IF EXISTS `{dbName}`";
            await using var cmd = new MySqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();

            _logger.LogInformation("项目数据库 {DatabaseName} 删除成功", dbName);
            return (true, $"数据库 {dbName} 删除成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除项目数据库失败: {DatabaseName}", dbName);
            return (false, $"删除失败: {ex.Message}");
        }
    }
}