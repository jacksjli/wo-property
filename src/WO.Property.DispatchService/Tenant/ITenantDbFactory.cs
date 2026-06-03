namespace WO.Property.DispatchService.Tenant;

/// <summary>
/// 租户数据库工厂接口
/// </summary>
public interface ITenantDbFactory
{
    /// <summary>
    /// 获取当前租户代码
    /// </summary>
    string? GetCurrentTenantCode();

    /// <summary>
    /// 设置当前租户代码
    /// </summary>
    void SetCurrentTenantCode(string tenantCode);

    /// <summary>
    /// 清除当前租户代码
    /// </summary>
    void Clear();

    /// <summary>
    /// 获取指定租户的连接字符串
    /// </summary>
    string GetTenantConnectionString(string tenantCode);
}