using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WO.Property.Shared.Configuration;
using WO.Property.Shared.Tenant;

namespace WO.Property.Shared.Middleware;

/// <summary>
/// 项目路由中间件
/// 从 X-Project Header 或 JWT Claims 提取项目代码
/// 并注入到 ProjectDbFactory
/// </summary>
public class ProjectRoutingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ProjectRoutingMiddleware> _logger;

    // 不需要项目上下文的路径
    private static readonly string[] NoProjectPaths = new[]
    {
        "/health",
        "/api/auth/login",
        "/api/auth/register"
    };

    public ProjectRoutingMiddleware(RequestDelegate next, ILogger<ProjectRoutingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IProjectDbFactory projectDbFactory)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        // 检查是否是不需要项目的路径
        if (NoProjectPaths.Any(p => path.StartsWith(p.ToLower())))
        {
            await _next(context);
            return;
        }

        // 1. 尝试从 Header 获取项目代码
        var projectCode = context.Request.Headers["X-Project"].FirstOrDefault();

        // 2. 如果没有，尝试从 JWT Claims 获取
        if (string.IsNullOrEmpty(projectCode))
        {
            projectCode = context.User.Claims.FirstOrDefault(c => c.Type == "project_code")?.Value;
        }

        // 3. 如果还没有，返回错误
        if (string.IsNullOrEmpty(projectCode))
        {
            _logger.LogWarning("请求缺少 X-Project Header: {Path}", path);
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = $"缺少 X-Project Header，当前路径: {path}"
            });
            return;
        }

        // 4. 验证项目是否存在
        if (!projectDbFactory.ProjectExists(projectCode))
        {
            _logger.LogWarning("项目不存在: {ProjectCode}", projectCode);
            context.Response.StatusCode = 404;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = $"项目 [{projectCode}] 不存在"
            });
            return;
        }

        // 5. 设置当前项目
        projectDbFactory.SetCurrentProjectCode(projectCode);

        // 6. 在响应头中添加项目信息（方便调试）
        context.Response.Headers["X-Project"] = projectCode;

        _logger.LogDebug("请求 {Method} {Path} -> 项目: {ProjectCode}", context.Request.Method, path, projectCode);

        await _next(context);
    }
}

/// <summary>
/// 中间件扩展方法
/// </summary>
public static class ProjectRoutingMiddlewareExtensions
{
    public static IApplicationBuilder UseProjectRouting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ProjectRoutingMiddleware>();
    }
}