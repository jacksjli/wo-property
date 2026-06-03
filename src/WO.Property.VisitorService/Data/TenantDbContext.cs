using Microsoft.EntityFrameworkCore;
using WO.Property.VisitorService.Models;

namespace WO.Property.VisitorService.Data;

public class TenantDbContext : DbContext
{
    private readonly Tenant.ITenantDbFactory _tenantDbFactory;

    public DbSet<Visitor> Visitors => Set<Visitor>();
    public DbSet<ExternalPerson> ExternalPersons => Set<ExternalPerson>();

    public TenantDbContext(
        DbContextOptions<TenantDbContext> options,
        Tenant.ITenantDbFactory tenantDbFactory,
        ILogger<TenantDbContext> logger)
        : base(options)
    {
        _tenantDbFactory = tenantDbFactory;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Visitor>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Visitors");
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.VisitorName).HasColumnName("VisitorName").HasMaxLength(50);
            entity.Property(e => e.VisitorPhone).HasColumnName("VisitorPhone").HasMaxLength(20);
            entity.Property(e => e.IdCardNumber).HasColumnName("IdCardNumber").HasMaxLength(30);
            entity.Property(e => e.VisitPurpose).HasColumnName("VisitPurpose").HasMaxLength(50);
            entity.Property(e => e.VisitDate).HasColumnName("VisitDate");
            entity.Property(e => e.VisitTime).HasColumnName("VisitTime");
            entity.Property(e => e.LeaveTime).HasColumnName("LeaveTime");
            entity.Property(e => e.BuildingId).HasColumnName("BuildingId");
            entity.Property(e => e.RoomId).HasColumnName("RoomId");
            entity.Property(e => e.HostName).HasColumnName("HostName").HasMaxLength(50);
            entity.Property(e => e.HostPhone).HasColumnName("HostPhone").HasMaxLength(20);
            entity.Property(e => e.Status).HasColumnName("Status").HasMaxLength(20);
            entity.Property(e => e.Remarks).HasColumnName("Remarks");
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt");
            entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt");
            entity.Property(e => e.ProjectCode).HasColumnName("project_code");
        });

        modelBuilder.Entity<ExternalPerson>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("external_persons");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100);
            entity.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(20);
            entity.Property(e => e.Type).HasColumnName("type").HasMaxLength(20);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });
    }
}
