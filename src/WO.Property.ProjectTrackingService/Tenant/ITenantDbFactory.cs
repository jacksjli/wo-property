namespace WO.Property.ProjectTrackingService.Tenant;

public interface ITenantDbFactory
{
    string GetTenantConnectionString();
}
