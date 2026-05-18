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
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.Unit).HasColumnName("unit");
            entity.Property(e => e.UnitPrice).HasColumnName("unit_price").HasColumnType("decimal(18,2)");
            entity.Property(e => e.SafetyStock).HasColumnName("safety_stock");
            entity.Property(e => e.MaxStock).HasColumnName("max_stock");
            entity.Property(e => e.CurrentStock).HasColumnName("current_stock");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.UpdatedAt);
            entity.Ignore(e => e.IsDeleted);
        });

        modelBuilder.Entity<MaterialCategory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("material_categories");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.UpdatedAt);
            entity.Ignore(e => e.IsDeleted);
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