using System.Text.Json;

namespace WO.Property.PaymentService.Tenant;

/// <summary>
/// 租户配置加载器
/// 从 config/tenant-mapping.json 读取租户配置
/// </summary>
public class TenantConfigLoader
{
    private readonly string _configPath;
    private TenantConfig? _config;

    public TenantConfigLoader(IWebHostEnvironment env)
    {
        // config 目录在项目根目录下
        // ContentRootPath: src/WO.Property.PaymentService
        // 往上2层到 WO-Property-Management/
        var projectRoot = Path.Combine(env.ContentRootPath, "..", "..", "config");
        _configPath = Path.GetFullPath(projectRoot);
    }

    public TenantConfig GetConfig()
    {
        if (_config != null) return _config;

        var configFile = Path.Combine(_configPath, "tenant-mapping.json");
        if (!File.Exists(configFile))
        {
            throw new FileNotFoundException($"租户配置文件不存在: {configFile}");
        }

        var json = File.ReadAllText(configFile);
        _config = JsonSerializer.Deserialize<TenantConfig>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("租户配置文件格式错误");

        return _config;
    }

    public string GetDatabaseName(string tenantCode)
    {
        var config = GetConfig();

        if (config.DatabaseMode == "single")
        {
            // single 模式：所有租户共用一个数据库
            return config.Database.Server;
        }

        // multi 模式：每个租户独立数据库，使用配置中的 databaseName
        if (!config.Tenants.ContainsKey(tenantCode))
        {
            throw new InvalidOperationException($"租户 [{tenantCode}] 不存在于配置中");
        }

        return config.Tenants[tenantCode].DatabaseName;
    }

    public string BuildConnectionString(string tenantCode)
    {
        var config = GetConfig();
        var dbName = GetDatabaseName(tenantCode);

        return $"Server={config.Database.Server};Port={config.Database.Port};Database={dbName};User={config.Database.Username};Password={config.Database.Password};CharSet={config.Database.Charset};";
    }

    public IEnumerable<string> GetAllTenantCodes()
    {
        return GetConfig().Tenants.Keys;
    }

    public bool TenantExists(string tenantCode)
    {
        return GetConfig().Tenants.ContainsKey(tenantCode);
    }
}

public class TenantConfig
{
    public string Project { get; set; } = "";
    public string ProjectName { get; set; } = "";
    public string DatabaseMode { get; set; } = "single";
    public string DefaultTenant { get; set; } = "";
    public DatabaseConfig Database { get; set; } = new();
    public Dictionary<string, TenantInfo> Tenants { get; set; } = new();
}

public class DatabaseConfig
{
    public string Type { get; set; } = "mysql";
    public string Server { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 3306;
    public string Username { get; set; } = "root";
    public string Password { get; set; } = "";
    public string Charset { get; set; } = "utf8mb4";
}

public class TenantInfo
{
    public string DisplayName { get; set; } = "";
    public string DatabaseName { get; set; } = "";
    public string Status { get; set; } = "Active";
}