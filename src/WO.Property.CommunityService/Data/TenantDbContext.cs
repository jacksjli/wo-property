using Microsoft.EntityFrameworkCore;
using WO.Property.CommunityService.Models;

namespace WO.Property.CommunityService.Data;

public class TenantDbContext : DbContext
{
    private readonly Tenant.ITenantDbFactory _tenantDbFactory;
    private readonly ILogger<TenantDbContext> _logger;

    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<ActivityEnrollment> ActivityEnrollments => Set<ActivityEnrollment>();
    public DbSet<Notice> Notices => Set<Notice>();
    public DbSet<Suggestion> Suggestions => Set<Suggestion>();

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
            entity.ToTable("CommunityActivities");
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.Name).HasColumnName("ActivityTitle");
            entity.Property(e => e.Description).HasColumnName("Description");
            entity.Property(e => e.Location).HasColumnName("Location");
            entity.Property(e => e.StartTime).HasColumnName("StartTime");
            entity.Property(e => e.EndTime).HasColumnName("EndTime");
            entity.Property(e => e.MaxParticipants).HasColumnName("MaxParticipants");
            entity.Property(e => e.CurrentParticipants).HasColumnName("ParticipantCount");
            entity.Property(e => e.Status).HasColumnName("Status");
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").ValueGeneratedOnAdd();
            entity.Ignore(e => e.CoverImage);
            entity.Ignore(e => e.Remark);
            entity.Ignore(e => e.ProjectId);
            entity.Ignore(e => e.CreatedBy);
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

        modelBuilder.Entity<Notice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("CommunityNotices");
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.Title).HasColumnName("Title");
            entity.Property(e => e.Content).HasColumnName("Content");
            entity.Property(e => e.Type).HasColumnName("Type");
            entity.Property(e => e.Status).HasColumnName("Status");
            entity.Property(e => e.ProjectId).HasColumnName("ProjectId");
            entity.Property(e => e.Top).HasColumnName("Top");
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").ValueGeneratedOnAdd();
            entity.Ignore(e => e.CreatedBy);
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.UpdatedAt);
            entity.Ignore(e => e.IsDeleted);
        });

        modelBuilder.Entity<Suggestion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("CommunitySuggestions");
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.ProjectId).HasColumnName("ProjectId");
            entity.Property(e => e.Title).HasColumnName("Title");
            entity.Property(e => e.Content).HasColumnName("Content");
            entity.Property(e => e.ContactName).HasColumnName("ContactName");
            entity.Property(e => e.ContactPhone).HasColumnName("ContactPhone");
            entity.Property(e => e.Status).HasColumnName("Status");
            entity.Property(e => e.Reply).HasColumnName("Reply");
            entity.Property(e => e.RepliedAt).HasColumnName("RepliedAt");
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").ValueGeneratedOnAdd();
            entity.Ignore(e => e.CreatedBy);
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.UpdatedAt);
            entity.Ignore(e => e.IsDeleted);
        });
    }
}