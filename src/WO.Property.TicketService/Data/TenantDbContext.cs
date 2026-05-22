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
            entity.Property(e => e.TicketCode).HasColumnName("TicketNumber");
            entity.Property(e => e.Title).HasColumnName("Title");
            entity.Property(e => e.Description).HasColumnName("Description");
            entity.Property(e => e.Category).HasColumnName("category");
            entity.Property(e => e.Priority).HasColumnName("Priority");
            entity.Property(e => e.Status).HasColumnName("Status");
            entity.Property(e => e.CreatorPersonId).HasColumnName("creator_id");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.TicketTypeId).HasColumnName("ticket_type_id");
            entity.Property(e => e.AreaId).HasColumnName("area_id");
            entity.Property(e => e.BuildingId).HasColumnName("BuildingId");
            entity.Property(e => e.RoomId).HasColumnName("RoomId");
            entity.Property(e => e.ContactPersonName).HasColumnName("ContactPersonName");
            entity.Property(e => e.ContactPhone).HasColumnName("ContactPhone");
            entity.Property(e => e.Location).HasColumnName("Location");

            // 数据库自动生成的时间戳，不参与 INSERT/UPDATE
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt").ValueGeneratedOnUpdate();

            entity.Ignore(e => e.Images);
            entity.Ignore(e => e.CreatedBy);
            entity.Ignore(e => e.Rating);
            entity.Ignore(e => e.AssigneePersonId);
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.IsDeleted);
            entity.Ignore(e => e.JobTypeIds);
        });
    }
}