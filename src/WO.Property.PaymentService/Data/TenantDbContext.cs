using Microsoft.EntityFrameworkCore;
using WO.Property.PaymentService.Models;

namespace WO.Property.PaymentService.Data;

/// <summary>
/// 租户 DbContext for PaymentService
/// 表结构: bills, meter_readings
/// </summary>
public class TenantDbContext : DbContext
{
    private readonly Tenant.ITenantDbFactory _tenantDbFactory;
    private readonly ILogger<TenantDbContext> _logger;

    public DbSet<Bill> Bills => Set<Bill>();
    public DbSet<MeterReading> MeterReadings => Set<MeterReading>();

    public TenantDbContext(
        DbContextOptions<TenantDbContext> options,
        Tenant.ITenantDbFactory tenantDbFactory,
        ILogger<TenantDbContext> logger)
        : base(options)
    {
        _tenantDbFactory = tenantDbFactory;
        _logger = logger;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Bill>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("bills");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BillNo).HasColumnName("bill_no");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Unit).HasColumnName("unit");
            entity.Property(e => e.Amount).HasColumnName("amount");
            entity.Property(e => e.BillingMonth).HasColumnName("billing_month");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.PaidAt).HasColumnName("paid_at");
            entity.Property(e => e.PaymentMethod).HasColumnName("payment_method");
            entity.Property(e => e.TransactionId).HasColumnName("transaction_id");
            entity.Property(e => e.Remark).HasColumnName("remark");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.UpdatedAt);
            entity.Ignore(e => e.IsDeleted);
        });

        modelBuilder.Entity<MeterReading>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("meter_readings");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Unit).HasColumnName("unit");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.PreviousReading).HasColumnName("previous_reading");
            entity.Property(e => e.CurrentReading).HasColumnName("current_reading");
            entity.Property(e => e.Usage).HasColumnName("usage");
            entity.Property(e => e.Rate).HasColumnName("rate");
            entity.Property(e => e.Amount).HasColumnName("amount");
            entity.Property(e => e.ReadingDate).HasColumnName("reading_date");
            entity.Property(e => e.ReaderName).HasColumnName("reader_name");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.UpdatedAt);
            entity.Ignore(e => e.IsDeleted);
        });
    }
}