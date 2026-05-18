using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using WO.Property.AnnouncementService.Models;

namespace WO.Property.AnnouncementService.Data;

/// <summary>
/// 支持动态切换租户库的 DbContext
/// 实体映射到 tenant_a/tenant_b 数据库的 announcements 表
/// 表结构: Id, Title, Content, Type, IsTop, IsActive(TINYINT), Priority, StartDate, EndDate, CreatedBy(INT), CreatedAt
/// </summary>
public class TenantDbContext : DbContext
{
    private readonly Tenant.ITenantDbFactory _tenantDbFactory;
    private readonly ILogger<TenantDbContext> _logger;

    public DbSet<Announcement> Announcements => Set<Announcement>();

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
        modelBuilder.Entity<Announcement>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("announcements");

            // Column name mappings (camelCase model → PascalCase DB column)
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.Title).HasColumnName("Title");
            entity.Property(e => e.Content).HasColumnName("Content");
            entity.Property(e => e.Category).HasColumnName("Type");
            entity.Property(e => e.Level).HasColumnName("Priority");
            entity.Property(e => e.IsPinned).HasColumnName("IsTop");
            entity.Property(e => e.Status).HasColumnName("IsActive")
                .HasConversion(
                    v => v == "published" ? (byte)1 : (byte)0,
                    v => v == 1 ? "published" : "draft");
            entity.Property(e => e.StartTime).HasColumnName("StartDate");
            entity.Property(e => e.EndTime).HasColumnName("EndDate");
            entity.Property(e => e.CreatedBy).HasColumnName("CreatedBy")
                .HasConversion(
                    v => string.IsNullOrEmpty(v) ? (int?)null : int.Parse(v),
                    v => v.HasValue ? v.Value.ToString() : null);
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").ValueGeneratedOnAdd();

            // Ignore all properties not in the actual table
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.UpdatedAt);
            entity.Ignore(e => e.IsDeleted);
            entity.Ignore(e => e.AttachmentUrls);
            entity.Ignore(e => e.TargetBuildings);
            entity.Ignore(e => e.Publisher);
            entity.Ignore(e => e.PublishTime);
            entity.Ignore(e => e.ViewCount);
            entity.Ignore(e => e.ProjectId);
        });
    }
}