using Microsoft.EntityFrameworkCore;
using WO.Property.CommunityService.Models;

namespace WO.Property.CommunityService.Data;

public class TenantDbContext : DbContext
{
    private readonly Tenant.ITenantDbFactory _tenantDbFactory;
    private readonly ILogger<TenantDbContext> _logger;

    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<ActivityEnrollment> ActivityEnrollments => Set<ActivityEnrollment>();

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
        modelBuilder.Entity<Activity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("activities");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Location).HasColumnName("location");
            entity.Property(e => e.StartTime).HasColumnName("start_time");
            entity.Property(e => e.EndTime).HasColumnName("end_time");
            entity.Property(e => e.MaxParticipants).HasColumnName("max_participants");
            entity.Property(e => e.CurrentParticipants).HasColumnName("current_participants");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.CoverImage).HasColumnName("cover_image");
            entity.Property(e => e.Remark).HasColumnName("remark");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.UpdatedAt);
            entity.Ignore(e => e.IsDeleted);
        });

        modelBuilder.Entity<ActivityEnrollment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("activity_enrollments");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ActivityId).HasColumnName("activity_id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Phone).HasColumnName("phone");
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.EnrolledAt).HasColumnName("enrolled_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.UpdatedAt);
            entity.Ignore(e => e.IsDeleted);
        });
    }
}