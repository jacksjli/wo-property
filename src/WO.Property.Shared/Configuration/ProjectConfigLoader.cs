using System.Text.Json;

namespace WO.Property.Shared.Configuration;

public class ProjectConfig
{
    public string Project { get; set; } = string.Empty;
    public string Version { get; set; } = "2.0";
    public DatabaseConfig Database { get; set; } = new();
    public string CenterDatabase { get; set; } = "project_center";
    public Dictionary<string, ProjectInfo> Projects { get; set; } = new();
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

public class ProjectInfo
{
    public string DisplayName { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
    public Dictionary<string, object> Config { get; set; } = new();
}

public class ProjectConfigLoader
{
    private readonly ProjectConfig _config;
    private readonly string _configPath;

    public ProjectConfigLoader(string configPath)
    {
        _configPath = configPath;
        var json = File.ReadAllText(configPath);
        _config = JsonSerializer.Deserialize<ProjectConfig>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Failed to load project config");
    }

    public bool ProjectExists(string projectCode)
    {
        return _config.Projects.ContainsKey(projectCode);
    }

    public string GetDatabaseName(string projectCode)
    {
        if (!_config.Projects.TryGetValue(projectCode, out var info))
        {
            throw new InvalidOperationException($"Project [{projectCode}] not found");
        }
        return info.DatabaseName;
    }

    public string GetConnectionString(string projectCode)
    {
        var dbName = GetDatabaseName(projectCode);
        return $"Server={_config.Database.Server};Port={_config.Database.Port};Database={dbName};User={_config.Database.Username};Password={_config.Database.Password};CharSet=utf8mb4;";
    }

    public string GetCenterConnectionString()
    {
        return $"Server={_config.Database.Server};Port={_config.Database.Port};Database={_config.CenterDatabase};User={_config.Database.Username};Password={_config.Database.Password};CharSet=utf8mb4;";
    }

    public IEnumerable<string> GetAllProjectCodes()
    {
        return _config.Projects.Keys;
    }

    public ProjectInfo? GetProjectInfo(string projectCode)
    {
        return _config.Projects.TryGetValue(projectCode, out var info) ? info : null;
    }
}