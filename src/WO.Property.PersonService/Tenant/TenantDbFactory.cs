using System.Collections.Concurrent;
using MySqlConnector;

namespace WO.Property.PersonService.Tenant;

/// <summary>
/// 租户数据库工厂实现
/// 单租户多项目架构: project_code → database_name (从 center_db 查询)
/// </summary>
public class TenantDbFactory : ITenantDbFactory
{
    private readonly ILogger<TenantDbFactory> _logger;
    private readonly string _centerDbConnectionString;
    private readonly string _defaultConnectionString;
    private static readonly AsyncLocal<string?> _currentTenantCode = new();
    private static readonly ConcurrentDictionary<string, string> _projectDbCache = new();

    public TenantDbFactory(IConfiguration configuration, ILogger<TenantDbFactory> logger)
    {
        _logger = logger;
        _centerDbConnectionString = "Server=127.0.0.1;Port=3306;Database=center_db;User=root;Password=;CharSet=utf8mb4;";
        _defaultConnectionString = "Server=127.0.0.1;Port=3306;Database=wo_property;User=root;Password=;CharSet=utf8mb4;";
    }

    public string? GetCurrentTenantCode() => _currentTenantCode.Value;

    public void SetCurrentTenantCode(string? tenantCode)
    {
        _currentTenantCode.Value = tenantCode;
        _logger.LogDebug("TenantContext set: {TenantCode}", tenantCode);
    }

    public void Clear()
    {
        _currentTenantCode.Value = null;
    }

    private string ResolveProjectToDatabase(string projectCode)
    {
        if (string.IsNullOrEmpty(projectCode))
            return "wo_property";

        if (_projectDbCache.TryGetValue(projectCode, out var cached))
            return cached;

        try
        {
            using var connection = new MySqlConnection(_centerDbConnectionString);
            connection.Open();

            using var cmd = new MySqlCommand(
                "'wo_property'",
                connection);
            cmd.Parameters.AddWithValue("@code", projectCode);

            var result = cmd.ExecuteScalar();
            var dbName = string.IsNullOrEmpty(result?.ToString()) ? "wo_property" : result.ToString()!;

            _projectDbCache[projectCode] = dbName;
            _logger.LogDebug("Resolved project {ProjectCode} -> database {DatabaseName}", projectCode, dbName);
            return dbName;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve project {ProjectCode}, using default wo_property", projectCode);
            return "wo_property";
        }
    }

    public string GetTenantConnectionString(string tenantCode)
    {
        var databaseName = ResolveProjectToDatabase(tenantCode);

        var result = System.Text.RegularExpressions.Regex.Replace(
            _defaultConnectionString,
            @"Database\s*=\s*[^;]+",
            $"Database={databaseName}",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        _logger.LogDebug("Generated connection string for tenant {TenantCode} -> database {Database}", tenantCode, databaseName);
        return result;
    }
}
