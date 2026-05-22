using System.Text.Json;
using MySqlConnector;

namespace WO.Property.ContractService.Tenant;

/// <summary>
/// 租户配置加载器
/// 从 project_center 数据库动态获取项目配置
/// </summary>
public class TenantConfigLoader
{
    private readonly string _contentRootPath;
    private readonly string _centerDbConnectionString;
    private readonly string _configPath;

    public TenantConfigLoader(IWebHostEnvironment env)
    {
        _contentRootPath = env.ContentRootPath;
        
        // CenterService runs on port 5016
        // We need to get database info from project_center
        var projectRoot = Path.Combine(env.ContentRootPath, "..", "..");
        var configPath = Path.Combine(projectRoot, "config", "tenant-mapping.json");
        _configPath = configPath;
        
        // 读取基础配置获取数据库连接
        if (File.Exists(configPath))
        {
            var json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<TenantConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            if (config != null)
            {
                var db = config.Database;
                _centerDbConnectionString = $"Server={db.Server};Port={db.Port};Database={config.CenterDatabase};User={db.Username};Password={db.Password};CharSet={db.Charset};";
            }
        }
    }

    /// <summary>
    /// 从 project_center 数据库获取所有项目的数据库映射
    /// </summary>
    private Dictionary<string, TenantInfo> LoadTenantsFromDatabase()
    {
        var tenants = new Dictionary<string, TenantInfo>();
        
        if (string.IsNullOrEmpty(_centerDbConnectionString))
        {
            // 回退到配置文件
            Console.WriteLine("No center DB connection, falling back to config file");
            return LoadTenantsFromConfigFile();
        }
        
        try
        {
            using var connection = new MySqlConnection(_centerDbConnectionString);
            connection.Open();
            
            var command = new MySqlCommand(
                "SELECT code, database_name FROM projects WHERE status = 'active'", 
                connection);
            
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var code = reader.GetString("code");
                var databaseName = reader.GetString("database_name");
                tenants[code] = new TenantInfo
                {
                    DisplayName = code,
                    DatabaseName = databaseName,
                    Status = "active"
                };
            }
            
            Console.WriteLine($"Loaded {tenants.Count} tenants from database");
            
            // 如果数据库查询结果为空，使用配置文件作为回退
            if (tenants.Count == 0)
            {
                Console.WriteLine("No active tenants in DB, falling back to config file");
                return LoadTenantsFromConfigFile();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"从数据库加载租户配置失败: {ex.Message}");
            // 回退到配置文件
            return LoadTenantsFromConfigFile();
        }
        
        return tenants;
    }

    /// <summary>
    /// 从配置文件回退加载租户
    /// </summary>
    private Dictionary<string, TenantInfo> LoadTenantsFromConfigFile()
    {
        var tenants = new Dictionary<string, TenantInfo>();
        var configFile = Path.Combine(_configPath);
        
        if (!File.Exists(configFile))
        {
            return tenants;
        }
        
        var json = File.ReadAllText(configFile);
        var config = JsonSerializer.Deserialize<TenantConfig>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        if (config?.Tenants != null)
        {
            return config.Tenants;
        }
        
        return tenants;
    }

    public string GetDatabaseName(string tenantCode)
    {
        var tenants = LoadTenantsFromDatabase();
        
        if (!tenants.ContainsKey(tenantCode))
        {
            throw new InvalidOperationException($"租户 [{tenantCode}] 不存在于配置中");
        }

        return tenants[tenantCode].DatabaseName;
    }

    public string BuildConnectionString(string tenantCode)
    {
        var tenants = LoadTenantsFromDatabase();
        
        if (!tenants.ContainsKey(tenantCode))
        {
            throw new InvalidOperationException($"租户 [{tenantCode}] 不存在于配置中");
        }
        
        var dbName = tenants[tenantCode].DatabaseName;
        
        // 重新构建连接字符串，使用正确的数据库名
        var configFile = Path.Combine(_configPath);
        if (File.Exists(configFile))
        {
            var json = File.ReadAllText(configFile);
            var config = JsonSerializer.Deserialize<TenantConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            if (config != null)
            {
                var db = config.Database;
                return $"Server={db.Server};Port={db.Port};Database={dbName};User={db.Username};Password={db.Password};CharSet={db.Charset};";
            }
        }
        
        throw new InvalidOperationException("无法构建连接字符串");
    }

    public IEnumerable<string> GetAllTenantCodes()
    {
        var tenants = LoadTenantsFromDatabase();
        return tenants.Keys;
    }

    public bool TenantExists(string tenantCode)
    {
        var tenants = LoadTenantsFromDatabase();
        return tenants.ContainsKey(tenantCode);
    }
}

public class TenantConfig
{
    public string Project { get; set; } = "";
    public string ProjectName { get; set; } = "";
    public string DatabaseMode { get; set; } = "single";
    public string DefaultTenant { get; set; } = "";
    public string CenterDatabase { get; set; } = "project_center";
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