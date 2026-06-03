using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;

namespace WO.Property.Shared.Configuration
{
    /// <summary>
    /// 服务启动配置助手
    /// 所有服务使用统一的端口配置
    /// </summary>
    public static class ServiceRunner
    {
        /// <summary>
        /// 配置服务使用 ports.json 中的端口
        /// </summary>
        public static void ConfigurePort(WebApplicationBuilder builder, string serviceName, int defaultPort)
        {
            var port = PortConfig.GetPortOrDefault(serviceName, defaultPort);
            builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
            Console.WriteLine($"[ServiceRunner] {serviceName} configured for port {port}");
        }

        /// <summary>
        /// 配置服务使用 ports.json 中的端口，无默认值
        /// </summary>
        public static void ConfigurePort(WebApplicationBuilder builder, string serviceName)
        {
            var port = PortConfig.GetPort(serviceName);
            if (port.HasValue)
            {
                builder.WebHost.UseUrls($"http://0.0.0.0:{port.Value}");
                Console.WriteLine($"[ServiceRunner] {serviceName} configured for port {port.Value}");
            }
            else
            {
                throw new InvalidOperationException($"Port not configured for {serviceName} in ports.json");
            }
        }
    }
}