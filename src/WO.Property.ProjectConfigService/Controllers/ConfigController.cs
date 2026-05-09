using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WO.Property.ProjectConfigService.Models;
using WO.Property.ProjectConfigService.Services;

namespace WO.Property.ProjectConfigService.Controllers;

[ApiController]
[Route("api/config")]
[Authorize]
public class ConfigController : ControllerBase
{
    private readonly IProjectConfigService _service;

    public ConfigController(IProjectConfigService service)
    {
        _service = service;
    }

    // ==================== 项目接口 ====================

    [HttpGet("projects")]
    public async Task<IActionResult> GetProjects()
    {
        var projects = await _service.GetProjectsAsync();
        return Ok(new { success = true, data = projects });
    }

    [HttpGet("projects/{id}")]
    public async Task<IActionResult> GetProject(int id)
    {
        var project = await _service.GetProjectByIdAsync(id);
        if (project == null)
            return NotFound(new { success = false, message = "项目不存在" });
        return Ok(new { success = true, data = project });
    }

    [HttpPost("projects")]
    public async Task<IActionResult> CreateProject([FromBody] JsonElement body)
    {
        var project = JsonSerializer.Deserialize<Project>(body.GetRawText())!;
        var created = await _service.CreateProjectAsync(project);
        return Ok(new { success = true, data = created });
    }

    // ==================== 项目配置接口 ====================

    /// <summary>
    /// 获取指定项目的某个配置
    /// </summary>
    [HttpGet("project/{projectId}/{module}/{configKey}")]
    public async Task<IActionResult> GetConfig(int projectId, string module, string configKey)
    {
        var config = await _service.GetConfigAsync(projectId, module, configKey);
        if (config == null)
            return Ok(new { success = true, data = (object?)null });

        return Ok(new
        {
            success = true,
            data = new
            {
                config.Id,
                config.ProjectId,
                config.Module,
                config.ConfigKey,
                config.ConfigValue,
                config.FieldConfig
            }
        });
    }

    /// <summary>
    /// 获取指定项目的所有配置（按模块）
    /// </summary>
    [HttpGet("project/{projectId}/module/{module}")]
    public async Task<IActionResult> GetConfigsByModule(int projectId, string module)
    {
        var configs = await _service.GetConfigsByModuleAsync(projectId, module);
        return Ok(new { success = true, data = configs });
    }

    /// <summary>
    /// 保存项目配置（新增或更新）
    /// </summary>
    [HttpPost("project/{projectId}/{module}/{configKey}")]
    public async Task<IActionResult> SaveConfig(int projectId, string module, string configKey, [FromBody] JsonElement body)
    {
        var configValue = body.GetProperty("configValue").GetString() ?? "{}";
        var fieldConfig = body.TryGetProperty("fieldConfig", out var fc) ? fc.GetString() : null;

        var config = new ProjectConfig
        {
            ProjectId = projectId,
            Module = module,
            ConfigKey = configKey,
            ConfigValue = configValue,
            FieldConfig = fieldConfig
        };

        var saved = await _service.SaveConfigAsync(config);
        return Ok(new { success = true, data = saved });
    }

    // ==================== 全局配置接口 ====================

    [HttpGet("global/{configKey}")]
    public async Task<IActionResult> GetGlobalConfig(string configKey)
    {
        var config = await _service.GetGlobalConfigAsync(configKey);
        if (config == null)
            return Ok(new { success = true, data = (object?)null });
        return Ok(new { success = true, data = config });
    }

    [HttpPost("global/{configKey}")]
    public async Task<IActionResult> SaveGlobalConfig(string configKey, [FromBody] JsonElement body)
    {
        var configValue = body.GetProperty("configValue").GetString() ?? "{}";
        var config = new GlobalConfig { ConfigKey = configKey, ConfigValue = configValue };
        var saved = await _service.SaveGlobalConfigAsync(config);
        return Ok(new { success = true, data = saved });
    }

    // ==================== 初始化接口 ====================

    [HttpPost("init")]
    public async Task<IActionResult> Initialize()
    {
        await _service.InitializeSampleDataAsync();
        return Ok(new { success = true, message = "初始化完成" });
    }
}
