using Microsoft.EntityFrameworkCore;
using WO.Property.ProjectConfigService.Data;
using WO.Property.ProjectConfigService.Models;

namespace WO.Property.ProjectConfigService.Services;

public class ProjectConfigServiceImpl : IProjectConfigService
{
    private readonly AppDbContext _db;

    public ProjectConfigServiceImpl(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Project>> GetProjectsAsync()
    {
        return await _db.Projects
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<Project?> GetProjectByIdAsync(int id)
    {
        return await _db.Projects
            .Include(p => p.Configs)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Project?> GetProjectByCodeAsync(string code)
    {
        return await _db.Projects
            .Include(p => p.Configs)
            .FirstOrDefaultAsync(p => p.Code == code);
    }

    public async Task<Project> CreateProjectAsync(Project project)
    {
        project.CreatedAt = DateTime.UtcNow;
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();
        return project;
    }

    public async Task<ProjectConfig?> GetConfigAsync(int projectId, string module, string configKey)
    {
        return await _db.ProjectConfigs
            .FirstOrDefaultAsync(c =>
                c.ProjectId == projectId &&
                c.Module == module &&
                c.ConfigKey == configKey);
    }

    public async Task<List<ProjectConfig>> GetConfigsByModuleAsync(int projectId, string module)
    {
        return await _db.ProjectConfigs
            .Where(c => c.ProjectId == projectId && c.Module == module)
            .ToListAsync();
    }

    public async Task<ProjectConfig> SaveConfigAsync(ProjectConfig config)
    {
        var existing = await _db.ProjectConfigs
            .FirstOrDefaultAsync(c =>
                c.ProjectId == config.ProjectId &&
                c.Module == config.Module &&
                c.ConfigKey == config.ConfigKey);

        if (existing != null)
        {
            existing.ConfigValue = config.ConfigValue;
            existing.FieldConfig = config.FieldConfig;
            existing.UpdatedAt = DateTime.UtcNow;
            _db.ProjectConfigs.Update(existing);
        }
        else
        {
            config.CreatedAt = DateTime.UtcNow;
            _db.ProjectConfigs.Add(config);
        }

        await _db.SaveChangesAsync();
        return existing ?? config;
    }

    public async Task<GlobalConfig?> GetGlobalConfigAsync(string configKey)
    {
        return await _db.GlobalConfigs
            .FirstOrDefaultAsync(c => c.ConfigKey == configKey);
    }

    public async Task<GlobalConfig> SaveGlobalConfigAsync(GlobalConfig config)
    {
        var existing = await _db.GlobalConfigs
            .FirstOrDefaultAsync(c => c.ConfigKey == config.ConfigKey);

        if (existing != null)
        {
            existing.ConfigValue = config.ConfigValue;
            existing.UpdatedAt = DateTime.UtcNow;
            _db.GlobalConfigs.Update(existing);
            await _db.SaveChangesAsync();
            return existing;
        }
        else
        {
            config.CreatedAt = DateTime.UtcNow;
            _db.GlobalConfigs.Add(config);
            await _db.SaveChangesAsync();
            return config;
        }
    }

    public async Task InitializeSampleDataAsync()
    {
        if (await _db.Projects.AnyAsync()) return;

        var project = new Project
        {
            Code = "WO_PROPERTY",
            Name = "WO物业管理系统",
            Status = "Active",
            Tier = "normal",
            CreatedAt = DateTime.UtcNow
        };
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        // 初始化默认字段配置
        var fieldConfigs = GetDefaultFieldConfigs(project.Id);
        _db.ProjectConfigs.AddRange(fieldConfigs);
        await _db.SaveChangesAsync();

        // 全局配置
        _db.GlobalConfigs.AddRange(new[]
        {
            new GlobalConfig { ConfigKey = "system.modules", ConfigValue = "[\"工单管理\",\"设备管理\",\"住户管理\",\"人员管理\",\"快递管理\",\"外卖管理\",\"公告管理\",\"门禁管理\",\"装修管理\"]" },
            new GlobalConfig { ConfigKey = "system.allowedOrigins", ConfigValue = "[\"http://localhost:5173\",\"http://localhost:5174\",\"http://localhost:5175\"]" },
        });
        await _db.SaveChangesAsync();
    }

    private static List<ProjectConfig> GetDefaultFieldConfigs(int projectId)
    {
        var configs = new List<ProjectConfig>();

        var defaultFields = new Dictionary<string, Dictionary<string, object[]>>
        {
            ["ticket"] = new()
            {
                ["default"] = new object[]
                {
                    new { id = 1, name = "工单编号", key = "ticketNo", type = "text", width = 120 },
                    new { id = 2, name = "类型", key = "type", type = "select", width = 100 },
                    new { id = 3, name = "优先级", key = "priority", type = "select", width = 80 },
                    new { id = 4, name = "状态", key = "status", type = "select", width = 80 },
                    new { id = 5, name = "创建人", key = "creatorName", type = "text", width = 100 },
                    new { id = 6, name = "创建时间", key = "createdAt", type = "datetime", width = 160 },
                }
            },
            ["resident"] = new()
            {
                ["default"] = new object[]
                {
                    new { id = 1, name = "姓名", key = "name", type = "text", width = 100 },
                    new { id = 2, name = "电话", key = "phone", type = "text", width = 130 },
                    new { id = 3, name = "楼栋", key = "buildingName", type = "text", width = 80 },
                    new { id = 4, name = "房号", key = "roomName", type = "text", width = 80 },
                    new { id = 5, name = "楼层", key = "floor", type = "number", width = 60 },
                    new { id = 6, name = "状态", key = "status", type = "select", width = 80 },
                }
            }
        };

        foreach (var (module, moduleConfigs) in defaultFields)
        {
            foreach (var (configKey, fields) in moduleConfigs)
            {
                configs.Add(new ProjectConfig
                {
                    ProjectId = projectId,
                    Module = module,
                    ConfigKey = configKey,
                    ConfigValue = "{}",
                    FieldConfig = System.Text.Json.JsonSerializer.Serialize(fields)
                });
            }
        }

        return configs;
    }
}
