using Microsoft.EntityFrameworkCore;
using WO.Property.ExpressService.Models;

namespace WO.Property.ExpressService.Data;

/// <summary>
/// 租户 DbContext for ExpressService
/// 表结构: ExpressRecords, express_companies
/// </summary>
public class TenantDbContext : DbContext
{
    private readonly Tenant.ITenantDbFactory _tenantDbFactory;

    public DbSet<ExpressRecord> ExpressRecords => Set<ExpressRecord>();
    public DbSet<ExpressCompany> ExpressCompanies => Set<ExpressCompany>();

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
        // ExpressRecords mapping
        modelBuilder.Entity<ExpressRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("ExpressRecords");
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.RoomId).HasColumnName("RoomId");
            entity.Property(e => e.RecipientName).HasColumnName("RecipientName").HasMaxLength(50);
            entity.Property(e => e.RecipientPhone).HasColumnName("RecipientPhone").HasMaxLength(20);
            entity.Property(e => e.CourierCompany).HasColumnName("CourierCompany").HasMaxLength(50);
            entity.Property(e => e.TrackingNumber).HasColumnName("TrackingNumber").HasMaxLength(100);
            entity.Property(e => e.PickupCode).HasColumnName("PickupCode").HasMaxLength(20);
            entity.Property(e => e.Status).HasColumnName("Status").HasMaxLength(20);
            entity.Property(e => e.PickupTime).HasColumnName("PickupTime");
            entity.Property(e => e.Remarks).HasColumnName("Remarks");
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt");
            entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt");
        });

        // ExpressCompanies mapping
        modelBuilder.Entity<ExpressCompany>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("express_companies");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.Phone).HasColumnName("phone");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
        });
    }
}
