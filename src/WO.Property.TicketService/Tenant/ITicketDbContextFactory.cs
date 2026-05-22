namespace WO.Property.TicketService.Tenant;

/// <summary>
/// 工单服务数据库上下文工厂
/// 支持 per-request 创建新实例，读取当前的 TenantCode
/// </summary>
public interface ITicketDbContextFactory
{
    AppDbContext CreateDbContext();
}
