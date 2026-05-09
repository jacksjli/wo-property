using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace YARP.Gateway.Authentication;

/// <summary>
/// JWT Token 生成和验证的共享工具类
/// 所有服务使用此工具类，确保 JWT 配置完全一致
/// </summary>
public static class JwtTokenHelper
{
    public static JwtSecurityTokenHandler TokenHandler { get; } = new();

    /// <summary>
    /// 生成 JWT Token（供 AuthService 使用）
    /// </summary>
    public static string GenerateToken(JwtTokenOptions options, IEnumerable<Claim> claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(options.ExpiryDays),
            signingCredentials: credentials
        );

        return TokenHandler.WriteToken(token);
    }

    /// <summary>
    /// 验证 Token 并返回 ClaimsPrincipal
    /// </summary>
    public static ClaimsPrincipal? ValidateToken(string token, JwtTokenOptions options)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SecretKey));

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = options.Issuer,
            ValidAudience = options.Audience,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            return TokenHandler.ValidateToken(token, parameters, out _);
        }
        catch
        {
            return null;
        }
    }
}

public class JwtTokenOptions
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiryDays { get; set; } = 7;
}

/// <summary>
/// 用于验证跨服务请求的简版 JWT 方案（服务间调用）
/// 不验证 audience（因为服务间调用不涉及前端 audience）
/// </summary>
public static class InternalJwtHelper
{
    private const string InternalIssuer = "wo-property-internal";
    private const string InternalSecret = "WO-Internal-Service-to-Service-Secret-Key-2026";

    public static string GenerateInternalToken(string serviceName)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(InternalSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("service", serviceName),
            new Claim("type", "internal"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: InternalIssuer,
            audience: InternalIssuer,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static bool ValidateInternalToken(string token)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(InternalSecret));
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = InternalIssuer,
            ValidAudience = InternalIssuer,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            new JwtSecurityTokenHandler().ValidateToken(token, parameters, out _);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

public static class JwtAuthenticationExtensions
{
    /// <summary>
    /// 在 AuthService 中注册统一 JWT 认证方案
    /// </summary>
    public static IServiceCollection AddUnifiedJwtAuthentication(
        this IServiceCollection services,
        JwtTokenOptions options)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwtOpts =>
            {
                jwtOpts.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = options.Issuer,
                    ValidAudience = options.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(options.SecretKey)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        return services;
    }
}
