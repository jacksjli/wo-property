using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WO.Property.Shared.Configuration;

namespace WO.Property.Shared.Tenant;

/// <summary>
/// 项目数据库工厂接口
/// </summary>
public interface IProjectDbFactory
{
    /// <summary>
    /// 获取当前项目代码
    /// </summary>
    string? GetCurrentProjectCode();

    /// <summary>
    /// 设置当前项目代码
    /// </summary>
    void SetCurrentProjectCode(string projectCode);

    /// <summary>
    /// 获取当前项目的连接字符串
    /// </summary>
    string GetConnectionString();

    /// <summary>
    /// 创建当前项目的 DbContext
    /// </summary>
    DbContext CreateDbContext();

    /// <summary>
    /// 获取所有项目代码
    /// </summary>
    IEnumerable<string> GetAllProjectCodes();

    /// <summary>
    /// 项目是否存在
    /// </summary>
    bool ProjectExists(string projectCode);
}

/// <summary>
/// 项目数据库工厂实现
/// 使用 AsyncLocal 存储当前请求的项目代码（线程安全）
/// </summary>
public class ProjectDbFactory : IProjectDbFactory
{
    private readonly ProjectConfigLoader _configLoader;
    private readonly ILogger<ProjectDbFactory> _logger;

    // 使用 AsyncLocal 实现线程安全的请求级别存储
    private static readonly AsyncLocal<string?> _currentProjectCode = new();

    // 连接字符串缓存
    private readonly ConcurrentDictionary<string, string> _connectionStringCache = new();

    public ProjectDbFactory(ProjectConfigLoader configLoader, ILogger<ProjectDbFactory> logger)
    {
        _configLoader = configLoader;
        _logger = logger;
    }

    public string? GetCurrentProjectCode() => _currentProjectCode.Value;

    public void SetCurrentProjectCode(string projectCode)
    {
        if (!_configLoader.ProjectExists(projectCode))
        {
            throw new InvalidOperationException($"项目 [{projectCode}] 不存在");
        }

        _currentProjectCode.Value = projectCode;
        _logger.LogDebug("切换到项目: {ProjectCode}", projectCode);
    }

    public string GetConnectionString()
    {
        var projectCode = GetCurrentProjectCode();
        if (string.IsNullOrEmpty(projectCode))
        {
            throw new InvalidOperationException("未设置项目代码，请先调用 SetCurrentProjectCode");
        }

        return _connectionStringCache.GetOrAdd(projectCode, code =>
        {
            _logger.LogDebug("为项目 [{ProjectCode}] 创建连接字符串", code);
            return _configLoader.GetConnectionString(code);
        });
    }

    public DbContext CreateDbContext()
    {
        var connectionString = GetConnectionString();
        var optionsBuilder = new DbContextOptionsBuilder<ProjectDbContext>();
        optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        return new ProjectDbContext(optionsBuilder.Options);
    }

    public IEnumerable<string> GetAllProjectCodes()
    {
        return _configLoader.GetAllProjectCodes();
    }

    public bool ProjectExists(string projectCode)
    {
        return _configLoader.ProjectExists(projectCode);
    }
}

/// <summary>
/// 项目数据库上下文（用于泛型约束）
/// </summary>
public class ProjectDbContext : DbContext
{
    public ProjectDbContext(DbContextOptions<ProjectDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
    }
}