using Microsoft.EntityFrameworkCore;
using WO.Property.MaterialService.Models;

namespace WO.Property.MaterialService.Data;

public class TenantDbContext : DbContext
{
    private readonly Tenant.ITenantDbFactory _tenantDbFactory;
    private readonly ILogger<TenantDbContext> _logger;

    public DbSet<Material> Materials => Set<Material>();
    public DbSet<MaterialCategory> MaterialCategories => Set<MaterialCategory>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();

    public TenantDbContext(DbContextOptions<TenantDbContext> options, Tenant.ITenantDbFactory tenantDbFactory, ILogger<TenantDbContext> logger)
        : base(options)
    {
        _tenantDbFactory = tenantDbFactory;
        _logger = logger;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Material>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("materials");
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.MaterialNo).HasColumnName("MaterialNo").HasMaxLength(50);
            entity.Property(e => e.Name).HasColumnName("Name").HasMaxLength(100);
            entity.Property(e => e.Spec).HasColumnName("Spec").HasMaxLength(100);
            entity.Property(e => e.Unit).HasColumnName("Unit").HasMaxLength(20);
            entity.Property(e => e.Quantity).HasColumnName("Quantity");
            entity.Property(e => e.MinQuantity).HasColumnName("MinQuantity");
            entity.Property(e => e.Price).HasColumnName("Price").HasColumnType("decimal(10,2)");
            entity.Property(e => e.Location).HasColumnName("Location").HasMaxLength(100);
            entity.Property(e => e.Status).HasColumnName("Status").HasMaxLength(20).HasConversion<string>();
            entity.Property(e => e.Supplier).HasColumnName("Supplier").HasMaxLength(100);
            entity.Property(e => e.PurchaseDate).HasColumnName("PurchaseDate");
            entity.Property(e => e.ExpirationDate).HasColumnName("ExpirationDate");
            entity.Property(e => e.Remark).HasColumnName("Remark");
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").ValueGeneratedOnAdd();
            entity.Ignore(e => e.CreatedBy);
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.UpdatedAt);
            entity.Ignore(e => e.IsDeleted);
        });

        modelBuilder.Entity<MaterialCategory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("MaterialCategories");
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.Name).HasColumnName("Name");
            entity.Property(e => e.Code).HasColumnName("Code");
            entity.Property(e => e.Description).HasColumnName("Description");
            entity.Property(e => e.CreatedBy).HasColumnName("CreatedBy");
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").ValueGeneratedOnAdd();
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.UpdatedAt);
            entity.Property(e => e.IsDeleted).HasColumnName("IsDeleted").HasDefaultValue(false);
        });

        modelBuilder.Entity<StockTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("stock_transactions");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.MaterialId).HasColumnName("material_id");
            entity.Property(e => e.TransactionType).HasColumnName("transaction_type");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.UnitPrice).HasColumnName("unit_price").HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalAmount).HasColumnName("total_amount").HasColumnType("decimal(18,2)");
            entity.Property(e => e.Operator).HasColumnName("operator");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.UpdatedAt);
            entity.Ignore(e => e.IsDeleted);
        });
    }
}