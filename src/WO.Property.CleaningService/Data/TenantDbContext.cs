using Microsoft.EntityFrameworkCore;
using WO.Property.CleaningService.Models;

namespace WO.Property.CleaningService.Data;

/// <summary>
/// 租户 DbContext for CleaningService
/// 表结构: cleaning_staff, cleaning_tasks, cleaning_records
/// </summary>
public class TenantDbContext : DbContext
{
    private readonly Tenant.ITenantDbFactory _tenantDbFactory;
    private readonly ILogger<TenantDbContext> _logger;

    public DbSet<CleaningStaff> CleaningStaff => Set<CleaningStaff>();
    public DbSet<CleaningTask> CleaningTasks => Set<CleaningTask>();
    public DbSet<CleaningRecord> CleaningRecords => Set<CleaningRecord>();

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
        // cleaning_staff mapping
        modelBuilder.Entity<CleaningStaff>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("cleaning_staff");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Phone).HasColumnName("phone");
            entity.Property(e => e.Area).HasColumnName("area");
            entity.Property(e => e.WorkShift).HasColumnName("work_shift");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.UpdatedAt);
            entity.Ignore(e => e.IsDeleted);
        });

        // cleaning_tasks mapping (实际对应 CleaningRecords 表)
        modelBuilder.Entity<CleaningTask>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("CleaningRecords");
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.BuildingId).HasColumnName("BuildingId");
            entity.Property(e => e.CleaningArea).HasColumnName("CleaningArea").HasMaxLength(100);
            entity.Property(e => e.CleanerName).HasColumnName("CleanerName").HasMaxLength(50);
            entity.Property(e => e.CleaningType).HasColumnName("CleaningType").HasMaxLength(20);
            entity.Property(e => e.PlanDate).HasColumnName("PlanDate");
            entity.Property(e => e.ActualDate).HasColumnName("ActualDate");
            entity.Property(e => e.Status).HasColumnName("Status").HasMaxLength(20).HasConversion<string>();
            entity.Property(e => e.QualityLevel).HasColumnName("QualityLevel").HasMaxLength(10);
            entity.Property(e => e.Remarks).HasColumnName("Remarks");
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").ValueGeneratedOnAdd();
            entity.Ignore(e => e.CreatedBy);
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.UpdatedAt);
            entity.Ignore(e => e.IsDeleted);
        });

        // cleaning_records mapping
        modelBuilder.Entity<CleaningRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("cleaning_records");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TaskId).HasColumnName("task_id");
            entity.Property(e => e.Result).HasColumnName("result");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.Photo).HasColumnName("photo");
            entity.Property(e => e.RecordTime).HasColumnName("record_time");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.UpdatedAt);
            entity.Ignore(e => e.IsDeleted);
        });
    }
}