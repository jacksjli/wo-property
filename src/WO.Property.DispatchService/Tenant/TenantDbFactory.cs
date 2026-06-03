using System.Collections.Concurrent;
using MySqlConnector;

namespace WO.Property.DispatchService.Tenant;

/// <summary>
/// 租户数据库工厂实现
/// 单租户多项目架构: project_code → database_name
/// </summary>
public class TenantDbFactory : ITenantDbFactory
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TenantDbFactory> _logger;
    private readonly string _centerDbConnectionString;
    private static readonly AsyncLocal<string?> _currentTenantCode = new();
    private static readonly ConcurrentDictionary<string, string> _projectDbCache = new();

    public TenantDbFactory(IConfiguration configuration, ILogger<TenantDbFactory> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _centerDbConnectionString = _configuration.GetConnectionString("CenterDb")
            ?? "Server=127.0.0.1;Port=3306;Database=center_db;User=root;Password=;CharSet=utf8mb4;";
    }

    public string? GetCurrentTenantCode() => _currentTenantCode.Value;

    public void SetCurrentTenantCode(string tenantCode)
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
                "SELECT database_name FROM center_db.project WHERE project_code = @code",
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

    public string GetTenantConnectionString(string projectCode)
    {
        var databaseName = ResolveProjectToDatabase(projectCode);

        var baseConnStr = _configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Default connection string not configured");

        var result = System.Text.RegularExpressions.Regex.Replace(
            baseConnStr,
            @"Database\s*=\s*[^;]+",
            $"Database={databaseName}",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        _logger.LogDebug("Generated connection string for project {ProjectCode} -> database {Database}", projectCode, databaseName);
        return result;
    }
}