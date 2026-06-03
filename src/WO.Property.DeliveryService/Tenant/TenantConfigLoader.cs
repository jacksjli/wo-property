using System.Text.Json;

namespace WO.Property.DeliveryService.Tenant;

/// <summary>
/// 租户配置加载器（简化版 - 单租户单数据库）
/// </summary>
public class TenantConfigLoader
{
    private readonly string _configPath;
    private TenantConfig? _config;

    public TenantConfigLoader(IWebHostEnvironment env)
    {
        var projectRoot = Path.Combine(env.ContentRootPath, "..", "..");
        _configPath = Path.Combine(projectRoot, "config", "tenant-mapping.json");
    }

    public TenantConfig GetConfig()
    {
        if (_config != null) return _config;
        
        if (!File.Exists(_configPath))
        {
            throw new FileNotFoundException($"配置文件不存在: {_configPath}");
        }
        
        var json = File.ReadAllText(_configPath);
        _config = JsonSerializer.Deserialize<TenantConfig>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        return _config ?? new TenantConfig();
    }

    public string GetDatabaseName(string tenantCode)
    {
        return GetConfig().Database?.Database ?? "wo_property";
    }

    public string BuildConnectionString(string tenantCode)
    {
        var db = GetConfig().Database;
        if (db == null) return "";
        return $"Server={db.Server};Port={db.Port};Database={db.Database};User={db.Username};Password={db.Password};CharSet={db.Charset};";
    }

    public IEnumerable<string> GetAllTenantCodes()
    {
        return new[] { "wo_property" };
    }

    public bool TenantExists(string tenantCode)
    {
        return tenantCode == "wo_property";
    }
}

public class TenantConfig
{
    public string Project { get; set; } = "";
    public string Version { get; set; } = "";
    public string Architecture { get; set; } = "";
    public DatabaseConfig? Database { get; set; }
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
