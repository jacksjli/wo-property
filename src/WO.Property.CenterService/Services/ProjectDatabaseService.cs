using MySqlConnector;
using System.Text;

namespace WO.Property.CenterService.Services;

/// <summary>
/// 项目数据库服务 - 负责创建新项目的数据库和表结构
/// </summary>
public class ProjectDatabaseService
{
    private readonly string _connectionString;
    private readonly ILogger<ProjectDatabaseService> _logger;

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
        var scriptPath = "/Users/mac/Projects/WO-Property-Management/init-scripts/project-init.sql";
        
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
                _logger.LogWarning("SQL 执行警告: {Message} | Statement: {Statement}", ex.Message, statement.Substring(0, Math.Min(50, statement.Length)));
            }
        }

        _logger.LogInformation("执行了 {Count} 条 SQL 语句", executedCount);
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

        var tables = new[] { "tickets", "ticket_types", "areas", "buildings", "rooms", "departments", "persons" };
        
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
