using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace WO.Property.Shared.Logging;

/// <summary>
/// 统一日志中间件 - 提供 Serilog 配置和请求/异常日志中间件
/// </summary>
public static class LoggingExtensions
{
    // Serilog 配置方法
    public static readonly ActivitySource ActivitySource = new("WO.Property");

    public static void ConfigureSerilog(LoggerConfiguration config, string serviceName)
    {
        var logsPath = Path.Combine(Directory.GetCurrentDirectory(), "logs");
        Directory.CreateDirectory(logsPath);

        config.MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("ServiceName", serviceName)
            .Enrich.WithProperty("Application", "WO-Property")
            .WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter(renderMessage: true, closingDelimiter: Environment.NewLine))
            .WriteTo.File(
                new Serilog.Formatting.Json.JsonFormatter(renderMessage: true, closingDelimiter: Environment.NewLine),
                Path.Combine(logsPath, $"{serviceName.ToLower()}-.log"),
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: 100 * 1024 * 1024,
                retainedFileCountLimit: 30,
                rollOnFileSizeLimit: true,
                shared: false,
                flushToDiskInterval: TimeSpan.FromSeconds(2));
    }

    /// <summary>
    /// 添加请求日志中间件（每个API请求的入参/出参/耗时/状态码）
    /// </summary>
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var startTime = DateTime.UtcNow;
            var traceIdBytes = new byte[8];
            Random.Shared.NextBytes(traceIdBytes);
            var traceIdStr = Convert.ToHexString(traceIdBytes).ToLowerInvariant();

            // 将 traceId 注入响应头
            context.Response.Headers["X-Trace-Id"] = traceIdStr;

            try
            {
                await next();

                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                Log.Information(
                    "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms [TraceId:{TraceId}]",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    elapsed,
                    traceIdStr);
            }
            catch (Exception ex)
            {
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                Log.Error(ex,
                    "HTTP {Method} {Path} failed after {ElapsedMs}ms [TraceId:{TraceId}]",
                    context.Request.Method,
                    context.Request.Path,
                    elapsed,
                    traceIdStr);
                throw;
            }
        });
    }

    /// <summary>
    /// 全局异常处理中间件
    /// </summary>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            try
            {
                await next();
            }
            catch (Exception ex)
            {
                var traceId = context.Response.Headers["X-Trace-Id"].ToString();
                Log.Error(ex,
                    "Unhandled exception [TraceId:{TraceId}] {Path}",
                    traceId,
                    context.Request.Path);

                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json; charset=utf-8";
                var responseJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    error = "InternalServerError",
                    message = "An unexpected error occurred",
                    traceId
                });
                await context.Response.WriteAsync(responseJson);
            }
        });
    }
}