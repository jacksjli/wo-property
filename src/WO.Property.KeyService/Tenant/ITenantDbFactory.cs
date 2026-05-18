namespace WO.Property.KeyService.Tenant;

/// <summary>
/// 租户数据库工厂接口
/// </summary>
public interface ITenantDbFactory
{
    string? GetCurrentTenantCode();
    void SetCurrentTenantCode(string tenantCode);
    void Clear();
}