using System.Text.Json;

namespace WO.Property.Shared.Configuration;

/// <summary>
/// 端口配置读取器 - 所有服务统一从 ports.json 读取端口
/// </summary>
public static class PortConfig
{
    private static readonly string ConfigPath = Path.Combine(
        Directory.GetCurrentDirectory(),
        "..", "..", "..", "config", "ports.json"
    );

    private static Dictionary<string, int>? _cachedPorts;

    /// <summary>
    /// 获取服务端口
    /// </summary>
    /// <param name="serviceName">服务名 (如 "AuthService", "TicketService")</param>
    /// <returns>端口号，如果未找到返回 null</returns>
    public static int? GetPort(string serviceName)
    {
        var ports = LoadPorts();
        return ports.TryGetValue(serviceName, out var port) ? port : null;
    }

    /// <summary>
    /// 获取服务端口，如果未找到则使用默认值
    /// </summary>
    public static int GetPortOrDefault(string serviceName, int defaultPort)
    {
        return GetPort(serviceName) ?? defaultPort;
    }

    /// <summary>
    /// 加载所有端口配置
    /// </summary>
    private static Dictionary<string, int> LoadPorts()
    {
        if (_cachedPorts != null)
            return _cachedPorts;

        _cachedPorts = new Dictionary<string, int>();

        try
        {
            var configFile = FindConfigFile();
            if (string.IsNullOrEmpty(configFile))
            {
                Console.WriteLine($"[PortConfig] Warning: ports.json not found, using default ports");
                return _cachedPorts;
            }

            var json = File.ReadAllText(configFile);
            var doc = JsonDocument.Parse(json);
            
            if (doc.RootElement.TryGetProperty("services", out var services))
            {
                foreach (var service in services.EnumerateArray())
                {
                    if (service.TryGetProperty("name", out var nameElement) &&
                        service.TryGetProperty("port", out var portElement))
                    {
                        var name = nameElement.GetString();
                        var port = portElement.GetInt32();
                        if (!string.IsNullOrEmpty(name))
                        {
                            _cachedPorts[name] = port;
                        }
                    }
                }
            }

            Console.WriteLine($"[PortConfig] Loaded {_cachedPorts.Count} service ports");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PortConfig] Error loading ports.json: {ex.Message}");
        }

        return _cachedPorts;
    }

    /// <summary>
    /// 查找配置文件路径
    /// </summary>
    private static string? FindConfigFile()
    {
        // 尝试多个可能的位置
        var possiblePaths = new[]
        {
            ConfigPath,
            Path.Combine(Directory.GetCurrentDirectory(), "config", "ports.json"),
            Path.Combine(AppContext.BaseDirectory, "config", "ports.json"),
            "/Users/mac/Projects/WO-Property-Management/config/ports.json"
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                Console.WriteLine($"[PortConfig] Found config at: {path}");
                return path;
            }
        }

        return null;
    }

    /// <summary>
    /// 清除缓存（用于测试或重新加载）
    /// </summary>
    public static void ClearCache()
    {
        _cachedPorts = null;
    }
}