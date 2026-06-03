using System.Text.Json;
using MySqlConnector;

namespace WO.Property.TicketService.Tenant;

/// <summary>
/// 租户配置加载器（简化版 - 单租户单数据库）
/// </summary>
public class TenantConfigLoader
{
    private readonly string _contentRootPath;
    private readonly string _connectionString;
    private readonly string _configPath;

    public TenantConfigLoader(IWebHostEnvironment env)
    {
        _contentRootPath = env.ContentRootPath;

        var projectRoot = Path.Combine(env.ContentRootPath, "..", "..");
        var configPath = Path.Combine(projectRoot, "config", "tenant-mapping.json");
        _configPath = configPath;

        // 读取配置构建连接字符串
        if (File.Exists(configPath))
        {
            var json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<TenantConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            if (config?.Database != null)
            {
                var db = config.Database;
                _connectionString = $"Server={db.Server};Port={db.Port};Database={db.Database};User={db.Username};Password={db.Password};CharSet={db.Charset};";
            }
            else
            {
                _connectionString = "";
            }
        }
        else
        {
            _connectionString = "";
        }
    }

    public string GetDatabaseName(string tenantCode)
    {
        // 单租户模式，直接返回配置的数据库名
        if (File.Exists(_configPath))
        {
            var json = File.ReadAllText(_configPath);
            var config = JsonSerializer.Deserialize<TenantConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return config?.Database?.Database ?? "wo_property";
        }
        return "wo_property";
    }

    public string BuildConnectionString(string tenantCode)
    {
        // 单租户模式，返回固定连接字符串
        return _connectionString;
    }

    public IEnumerable<string> GetAllTenantCodes()
    {
        // 单租户模式，只返回 wo_property
        return new[] { "wo_property" };
    }

    public bool TenantExists(string tenantCode)
    {
        // 单租户模式，只检查 wo_property
        return tenantCode == "wo_property";
    }
}

public class TenantConfig
{
    public string Project { get; set; } = "";
    public string Version { get; set; } = "";
    public string Architecture { get; set; } = "";
    public DatabaseConfig Database { get; set; } = new();
    public List<ProjectInfo> Projects { get; set; } = new();
}

public class DatabaseConfig
{
    public string Type { get; set; } = "mysql";
    public string Server { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 3306;
    public string Database { get; set; } = "wo_property";
    public string Username { get; set; } = "root";
    public string Password { get; set; } = "";
    public string Charset { get; set; } = "utf8mb4";
}

public class ProjectInfo
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Status { get; set; } = "active";
}