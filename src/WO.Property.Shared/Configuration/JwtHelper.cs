namespace WO.Property.Shared.Configuration;

/// <summary>
/// JWT 密钥统一访问助手（从环境变量 JWT_SECRET_KEY 读取）
/// </summary>
public static class JwtHelper
{
    private static readonly string DefaultSecret = "WO-Property-Management-Unified-Secret-Key-2026-For-All-Services";

    /// <summary>
    /// 获取 JWT 签名密钥
    /// </summary>
    public static string GetSecretKey()
    {
        var key = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
        return string.IsNullOrWhiteSpace(key) ? DefaultSecret : key;
    }

    /// <summary>
    /// 获取 JWT 发行方
    /// </summary>
    public static string GetIssuer() =>
        Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "wo-property-unified-auth";

    /// <summary>
    /// 获取 JWT 受众
    /// </summary>
    public static string GetAudience() =>
        Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "wo-property-services";
}
