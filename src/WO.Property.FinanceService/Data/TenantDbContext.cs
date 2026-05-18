using Microsoft.EntityFrameworkCore;
using WO.Property.FinanceService.Models;

namespace WO.Property.FinanceService.Data;

public class TenantDbContext : DbContext
{
    private readonly Tenant.ITenantDbFactory _tenantDbFactory;
    private readonly ILogger<TenantDbContext> _logger;

    public DbSet<FeeItem> FeeItems => Set<FeeItem>();
    public DbSet<Bill> Bills => Set<Bill>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Receipt> Receipts => Set<Receipt>();

    public TenantDbContext(DbContextOptions<TenantDbContext> options, Tenant.ITenantDbFactory tenantDbFactory, ILogger<TenantDbContext> logger)
        : base(options)
    {
        _tenantDbFactory = tenantDbFactory;
        _logger = logger;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FeeItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("fee_items");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Type).HasColumnName("type").HasConversion<string>();
            entity.Property(e => e.UnitPrice).HasColumnName("unit_price").HasColumnType("decimal(18,2)");
            entity.Property(e => e.Unit).HasColumnName("unit");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<Bill>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("bills");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BillNumber).HasColumnName("bill_number");
            entity.Property(e => e.RoomNumber).HasColumnName("room_number");
            entity.Property(e => e.OwnerName).HasColumnName("owner_name");
            entity.Property(e => e.OwnerPhone).HasColumnName("owner_phone");
            entity.Property(e => e.FeeType).HasColumnName("fee_type").HasConversion<string>();
            entity.Property(e => e.Amount).HasColumnName("amount").HasColumnType("decimal(18,2)");
            entity.Property(e => e.DiscountAmount).HasColumnName("discount_amount").HasColumnType("decimal(18,2)");
            entity.Property(e => e.ActualAmount).HasColumnName("actual_amount").HasColumnType("decimal(18,2)");
            entity.Property(e => e.Currency).HasColumnName("currency");
            entity.Property(e => e.BillingPeriodStart).HasColumnName("billing_period_start");
            entity.Property(e => e.BillingPeriodEnd).HasColumnName("billing_period_end");
            entity.Property(e => e.DueDate).HasColumnName("due_date");
            entity.Property(e => e.PaidDate).HasColumnName("paid_date");
            entity.Property(e => e.Status).HasColumnName("status").HasConversion<string>();
            entity.Property(e => e.Remarks).HasColumnName("remarks");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.UpdatedAt);
            entity.Ignore(e => e.IsDeleted);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("payments");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PaymentNumber).HasColumnName("payment_number");
            entity.Property(e => e.BillId).HasColumnName("bill_id");
            entity.Property(e => e.Amount).HasColumnName("amount").HasColumnType("decimal(18,2)");
            entity.Property(e => e.Currency).HasColumnName("currency");
            entity.Property(e => e.PaymentMethod).HasColumnName("payment_method").HasConversion<string>();
            entity.Property(e => e.PaymentDate).HasColumnName("payment_date");
            entity.Property(e => e.TransactionId).HasColumnName("transaction_id");
            entity.Property(e => e.Remarks).HasColumnName("remarks");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<Receipt>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("receipts");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ReceiptNumber).HasColumnName("receipt_number");
            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.PayerName).HasColumnName("payer_name");
            entity.Property(e => e.PayerPhone).HasColumnName("payer_phone");
            entity.Property(e => e.RoomNumber).HasColumnName("room_number");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
        });
    }
}