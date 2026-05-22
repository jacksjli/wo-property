using Microsoft.EntityFrameworkCore;
using WO.Property.DeviceService.Tenant;

namespace WO.Property.DeviceService.Data;

/// <summary>
/// 租户数据库上下文
/// </summary>
public class TenantDbContext : DbContext
{
    private readonly ITenantDbFactory _tenantDbFactory;
    private readonly ILogger<TenantDbContext> _logger;

    public DbSet<Device> Devices => Set<Device>();
    public DbSet<DeviceCategory> DeviceCategories => Set<DeviceCategory>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<MaintenanceRecord> MaintenanceRecords => Set<MaintenanceRecord>();

    public TenantDbContext(
        DbContextOptions<TenantDbContext> options,
        ITenantDbFactory tenantDbFactory,
        ILogger<TenantDbContext> logger) : base(options)
    {
        _tenantDbFactory = tenantDbFactory;
        _logger = logger;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("devices");

            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.Code).HasColumnName("DeviceCode").HasMaxLength(50);
            entity.Property(e => e.Name).HasColumnName("DeviceName").HasMaxLength(200);
            entity.Property(e => e.DeviceTypeId).HasColumnName("DeviceTypeId");
            entity.Property(e => e.BuildingId).HasColumnName("BuildingId");
            entity.Property(e => e.Floor).HasColumnName("Floor");
            entity.Property(e => e.Location).HasColumnName("Location");
            entity.Property(e => e.PurchaseDate).HasColumnName("PurchaseDate");
            entity.Property(e => e.Status).HasColumnName("Status").HasMaxLength(20).HasConversion<string>();
            entity.Property(e => e.Remarks).HasColumnName("Remarks");
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt").ValueGeneratedOnUpdate();

            // DB字段映射
            entity.Property(e => e.LastMaintenanceDate).HasColumnName("LastMaintenanceDate");
            entity.Property(e => e.NextMaintenanceDate).HasColumnName("NextMaintenanceDate");
            entity.Property(e => e.SupplierId).HasColumnName("SupplierId");

            // 忽略模型中不存在于DB的字段
            entity.Ignore(e => e.Model);
            entity.Ignore(e => e.SerialNumber);
            entity.Ignore(e => e.CurrentStatus);
            entity.Ignore(e => e.WarrantyEndDate);
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.IsDeleted);
        });

        modelBuilder.Entity<DeviceCategory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("device_categories");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("DeviceName").HasMaxLength(100);
            entity.Property(e => e.Code).HasColumnName("DeviceCode").HasMaxLength(50);
            entity.Property(e => e.Description).HasColumnName("description");
        });

        modelBuilder.Entity<Location>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("locations");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("DeviceName").HasMaxLength(100);
            entity.Property(e => e.Type).HasColumnName("type").HasMaxLength(20).HasConversion<string>();
            entity.Property(e => e.ParentId).HasColumnName("parent_id");
        });

        modelBuilder.Entity<MaintenanceRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("maintenance_records");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DeviceId).HasColumnName("device_id");
            entity.Property(e => e.MaintenanceType).HasColumnName("maintenance_type").HasMaxLength(50).HasConversion<string>();
            entity.Property(e => e.MaintenanceDate).HasColumnName("maintenance_date");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Technician).HasColumnName("technician").HasMaxLength(100);
            entity.Property(e => e.Cost).HasColumnName("cost").HasPrecision(10, 2);
            entity.Property(e => e.Hours).HasColumnName("hours").HasPrecision(10, 2);
            
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();

            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.IsDeleted);
        });
    }
}

// 实体类（与 wo_property.devices 表结构完全一致）
public class Device
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int? DeviceTypeId { get; set; }
    public int? BuildingId { get; set; }
    public int? Floor { get; set; }
    public string? Location { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public string Status { get; set; } = "Active";
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // DB 中的额外字段（模型中未使用）
    public DateTime? LastMaintenanceDate { get; set; }
    public DateTime? NextMaintenanceDate { get; set; }
    public int? SupplierId { get; set; }

    // 忽略的基类字段
    public int? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }

    // 模型中存在但DB不存在的字段（通过 EF 忽略）
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public string CurrentStatus { get; set; } = "Normal";
    public DateTime? WarrantyEndDate { get; set; }
}

public class DeviceCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class Location
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Building";
    public int? ParentId { get; set; }
}

public class MaintenanceRecord
{
    public int Id { get; set; }
    public int DeviceId { get; set; }
    public string MaintenanceType { get; set; } = string.Empty;
    public DateTime MaintenanceDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Technician { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public decimal Hours { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
}