namespace WO.Property.MobileService.Tenant;

/// <summary>
/// 租户数据库工厂接口
/// </summary>
public interface ITenantDbFactory
{
    /// <summary>
    /// 设置当前租户代码
    /// </summary>
    void SetCurrentTenantCode(string? tenantCode);
    
    /// <summary>
    /// 获取当前租户代码
    /// </summary>
    string? GetCurrentTenantCode();
    
    /// <summary>
    /// 获取当前租户的数据库连接字符串
    /// </summary>
    string GetTenantConnectionString();
    
    /// <summary>
    /// 清除租户上下文
    /// </summary>
    void Clear();
}