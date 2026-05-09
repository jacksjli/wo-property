namespace WO.Property.Shared.Configuration;

/// <summary>
/// 统一 JWT 配置（从环境变量或 appsettings.json 读取）
/// </summary>
public class JwtSettings
{
    /// <summary>签名密钥（建议通过环境变量 JWT_SECRET_KEY 设置）</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>发行方</summary>
    public string Issuer { get; set; } = "wo-property-unified-auth";

    /// <summary>受众</summary>
    public string Audience { get; set; } = "wo-property-services";

    /// <summary>Token 过期分钟数</summary>
    public int ExpiryMinutes { get; set; } = 120;

    /// <summary>
    /// 获取实际可用的密钥，优先从环境变量读取，回退到配置的默认值
    /// </summary>
    public string GetSecretKey()
    {
        var envKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
        return !string.IsNullOrWhiteSpace(envKey)
            ? envKey
            : (!string.IsNullOrWhiteSpace(SecretKey) ? SecretKey : string.Empty);
    }
}
