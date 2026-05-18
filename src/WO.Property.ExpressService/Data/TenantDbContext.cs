using Microsoft.EntityFrameworkCore;
using WO.Property.ExpressService.Models;

namespace WO.Property.ExpressService.Data;

/// <summary>
/// 租户 DbContext for ExpressService
/// 表结构: express_companies, express_deliveries, express_notifications
/// </summary>
public class TenantDbContext : DbContext
{
    private readonly Tenant.ITenantDbFactory _tenantDbFactory;

    public DbSet<ExpressCompany> ExpressCompanies => Set<ExpressCompany>();
    public DbSet<ExpressDelivery> ExpressDeliveries => Set<ExpressDelivery>();
    public DbSet<ExpressNotification> ExpressNotifications => Set<ExpressNotification>();

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
        // express_companies mapping
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
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.UpdatedAt);
            entity.Ignore(e => e.IsDeleted);
        });

        // express_deliveries mapping
        modelBuilder.Entity<ExpressDelivery>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("express_deliveries");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TrackingNumber).HasColumnName("tracking_number");
            entity.Property(e => e.ExpressNumber).HasColumnName("express_number");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.SenderName).HasColumnName("sender_name");
            entity.Property(e => e.SenderPhone).HasColumnName("sender_phone");
            entity.Property(e => e.ReceiverName).HasColumnName("receiver_name");
            entity.Property(e => e.ReceiverPhone).HasColumnName("receiver_phone");
            entity.Property(e => e.RoomNumber).HasColumnName("room_number");
            entity.Property(e => e.PickupAddress).HasColumnName("pickup_address");
            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasConversion<string>();
            entity.Property(e => e.Remarks).HasColumnName("remarks");
            entity.Property(e => e.ReceivedAt).HasColumnName("received_at");
            entity.Property(e => e.PickedUpAt).HasColumnName("picked_up_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.ReceivedBy).HasColumnName("received_by");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.UpdatedAt);
            entity.Ignore(e => e.IsDeleted);
            entity.Ignore(e => e.Company);
        });

        // express_notifications mapping
        modelBuilder.Entity<ExpressNotification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("express_notifications");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ExpressId).HasColumnName("express_id");
            entity.Property(e => e.Type)
                .HasColumnName("type")
                .HasConversion<string>();
            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasConversion<string>();
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.SentAt).HasColumnName("sent_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.UpdatedAt);
            entity.Ignore(e => e.IsDeleted);
            entity.Ignore(e => e.Express);
        });
    }
}