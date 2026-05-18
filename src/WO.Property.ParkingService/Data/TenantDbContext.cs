using Microsoft.EntityFrameworkCore;
using WO.Property.ParkingService.Models;

namespace WO.Property.ParkingService.Data;

/// <summary>
/// 租户 DbContext for ParkingService
/// 表结构: parking_lots, parking_spaces, vehicles, parking_records
/// </summary>
public class TenantDbContext : DbContext
{
    private readonly Tenant.ITenantDbFactory _tenantDbFactory;
    private readonly ILogger<TenantDbContext> _logger;

    public DbSet<ParkingLot> ParkingLots => Set<ParkingLot>();
    public DbSet<ParkingSpace> ParkingSpaces => Set<ParkingSpace>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<ParkingRecord> ParkingRecords => Set<ParkingRecord>();

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
        modelBuilder.Entity<ParkingLot>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("parking_lots");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Location).HasColumnName("location");
            entity.Property(e => e.TotalSpaces).HasColumnName("total_spaces");
            entity.Property(e => e.HourlyRate).HasColumnName("hourly_rate");
            entity.Property(e => e.MonthlyRate).HasColumnName("monthly_rate");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.UpdatedAt);
            entity.Ignore(e => e.IsDeleted);
            entity.Ignore(e => e.Spaces);
            entity.Ignore(e => e.Records);
        });

        modelBuilder.Entity<ParkingSpace>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("parking_spaces");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.SpaceNo).HasColumnName("space_no");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.LotId).HasColumnName("lot_id");
            entity.Property(e => e.VehicleId).HasColumnName("vehicle_id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.UpdatedAt);
            entity.Ignore(e => e.IsDeleted);
            entity.Ignore(e => e.Lot);
            entity.Ignore(e => e.Vehicle);
        });

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("vehicles");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PlateNumber).HasColumnName("plate_number");
            entity.Property(e => e.Brand).HasColumnName("brand");
            entity.Property(e => e.Color).HasColumnName("color");
            entity.Property(e => e.OwnerName).HasColumnName("owner_name");
            entity.Property(e => e.OwnerPhone).HasColumnName("owner_phone");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.PlateImage).HasColumnName("plate_image");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.UpdatedAt);
            entity.Ignore(e => e.IsDeleted);
        });

        modelBuilder.Entity<ParkingRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("parking_records");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PlateNumber).HasColumnName("plate_number");
            entity.Property(e => e.LotId).HasColumnName("lot_id");
            entity.Property(e => e.EntryTime).HasColumnName("entry_time");
            entity.Property(e => e.ExitTime).HasColumnName("exit_time");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.VehicleType).HasColumnName("vehicle_type");
            entity.Property(e => e.DurationMinutes).HasColumnName("duration_minutes");
            entity.Property(e => e.Fee).HasColumnName("fee");
            entity.Property(e => e.PaymentStatus).HasColumnName("payment_status");
            entity.Property(e => e.PaidAt).HasColumnName("paid_at");
            entity.Property(e => e.PaymentMethod).HasColumnName("payment_method");
            entity.Property(e => e.Remark).HasColumnName("remark");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.UpdatedAt);
            entity.Ignore(e => e.IsDeleted);
            entity.Ignore(e => e.Lot);
        });
    }
}