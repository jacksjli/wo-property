using Microsoft.EntityFrameworkCore;
using WO.Property.KeyService.Models;

namespace WO.Property.KeyService.Data;

/// <summary>
/// 租户 DbContext for KeyService
/// 表结构: keys, key_borrows, key_usage_logs
/// </summary>
public class TenantDbContext : DbContext
{
    private readonly Tenant.ITenantDbFactory _tenantDbFactory;
    private readonly ILogger<TenantDbContext> _logger;

    public DbSet<Key> Keys => Set<Key>();
    public DbSet<KeyBorrow> KeyBorrows => Set<KeyBorrow>();
    public DbSet<KeyUsageLog> KeyUsageLogs => Set<KeyUsageLog>();

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
        // keys mapping
        modelBuilder.Entity<Key>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("keys");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.KeyNumber).HasColumnName("key_number");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Location).HasColumnName("location");
            entity.Property(e => e.RoomNumber).HasColumnName("room_number");
            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasConversion<string>();
            entity.Property(e => e.TotalCopies).HasColumnName("total_copies");
            entity.Property(e => e.AvailableCopies).HasColumnName("available_copies");
            entity.Property(e => e.StorageLocation).HasColumnName("storage_location");
            entity.Property(e => e.Remarks).HasColumnName("remarks");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.UpdatedAt);
            entity.Ignore(e => e.IsDeleted);
        });

        // key_borrows mapping
        modelBuilder.Entity<KeyBorrow>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("key_borrows");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BorrowNumber).HasColumnName("borrow_number");
            entity.Property(e => e.KeyId).HasColumnName("key_id");
            entity.Property(e => e.BorrowerName).HasColumnName("borrower_name");
            entity.Property(e => e.BorrowerPhone).HasColumnName("borrower_phone");
            entity.Property(e => e.BorrowerUnit).HasColumnName("borrower_unit");
            entity.Property(e => e.BorrowDate).HasColumnName("borrow_date");
            entity.Property(e => e.ExpectedReturnDate).HasColumnName("expected_return_date");
            entity.Property(e => e.ActualReturnDate).HasColumnName("actual_return_date");
            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasConversion<string>();
            entity.Property(e => e.Purpose).HasColumnName("purpose");
            entity.Property(e => e.Approver).HasColumnName("approver");
            entity.Property(e => e.ApprovedDate).HasColumnName("approved_date");
            entity.Property(e => e.ApprovedRemarks).HasColumnName("approved_remarks");
            entity.Property(e => e.ReturnReceiver).HasColumnName("return_receiver");
            entity.Property(e => e.ReturnRemarks).HasColumnName("return_remarks");
            entity.Property(e => e.KeyReturned).HasColumnName("key_returned");
            entity.Property(e => e.KeyConditionOk).HasColumnName("key_condition_ok");
            entity.Property(e => e.KeyConditionRemarks).HasColumnName("key_condition_remarks");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.UpdatedAt);
            entity.Ignore(e => e.IsDeleted);
            entity.Ignore(e => e.Key);
        });

        // key_usage_logs mapping
        modelBuilder.Entity<KeyUsageLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("key_usage_logs");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.KeyId).HasColumnName("key_id");
            entity.Property(e => e.BorrowId).HasColumnName("borrow_id");
            entity.Property(e => e.Action).HasColumnName("action");
            entity.Property(e => e.Operator).HasColumnName("operator");
            entity.Property(e => e.Remarks).HasColumnName("remarks");
            entity.Property(e => e.ActionTime).HasColumnName("action_time");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.UpdatedAt);
            entity.Ignore(e => e.IsDeleted);
            entity.Ignore(e => e.Key);
        });
    }
}