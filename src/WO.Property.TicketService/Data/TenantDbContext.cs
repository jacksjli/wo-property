using Microsoft.EntityFrameworkCore;

namespace WO.Property.TicketService.Data;

/// <summary>
/// 支持动态切换租户库的 DbContext
/// </summary>
public class TenantDbContext : DbContext
{
    private readonly Tenant.ITenantDbFactory _tenantDbFactory;
    private readonly ILogger<TenantDbContext> _logger;

    public DbSet<Ticket> Tickets => Set<Ticket>();

    public TenantDbContext(
        DbContextOptions<TenantDbContext> options,
        Tenant.ITenantDbFactory tenantDbFactory,
        ILogger<TenantDbContext> logger)
        : base(options)
    {
        _tenantDbFactory = tenantDbFactory;
        _logger = logger;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var tenantCode = _tenantDbFactory.GetCurrentTenantCode();
            _logger.LogDebug("TenantDbContext configuring for tenant: {TenantCode}", tenantCode ?? "none");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("tickets");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TicketCode).HasColumnName("ticket_code");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Category).HasColumnName("category");
            entity.Property(e => e.Priority).HasColumnName("priority");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.CreatorPersonId).HasColumnName("created_by");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");

            // 数据库自动生成的时间戳，不参与 INSERT/UPDATE
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").ValueGeneratedOnUpdate();

            // 忽略 Ticket 类中存在但表中不存在的属性
            entity.Ignore(e => e.Location);
            entity.Ignore(e => e.Images);
            entity.Ignore(e => e.CreatedBy);
            entity.Ignore(e => e.Rating);
            entity.Ignore(e => e.ContactPersonName);
            entity.Ignore(e => e.ContactPhone);
            entity.Ignore(e => e.AssigneePersonId);
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.IsDeleted);
        });
    }
}