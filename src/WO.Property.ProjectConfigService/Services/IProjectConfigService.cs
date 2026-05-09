using WO.Property.ProjectConfigService.Models;

namespace WO.Property.ProjectConfigService.Services;

public interface IProjectConfigService
{
    // 项目 CRUD
    Task<List<Project>> GetProjectsAsync();
    Task<Project?> GetProjectByIdAsync(int id);
    Task<Project?> GetProjectByCodeAsync(string code);
    Task<Project> CreateProjectAsync(Project project);

    // 项目配置 CRUD
    Task<ProjectConfig?> GetConfigAsync(int projectId, string module, string configKey);
    Task<List<ProjectConfig>> GetConfigsByModuleAsync(int projectId, string module);
    Task<ProjectConfig> SaveConfigAsync(ProjectConfig config);

    // 全局配置
    Task<GlobalConfig?> GetGlobalConfigAsync(string configKey);
    Task<GlobalConfig> SaveGlobalConfigAsync(GlobalConfig config);

    // 初始化示例数据
    Task InitializeSampleDataAsync();
}
