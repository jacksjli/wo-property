using Microsoft.EntityFrameworkCore;
using WO.Property.ProjectTrackingService.Models;

namespace WO.Property.ProjectTrackingService.Data;

public class TenantDbContext : DbContext
{
    public TenantDbContext(DbContextOptions<TenantDbContext> options) : base(options)
    {
    }

    public DbSet<TrackingProject> Projects => Set<TrackingProject>();
    public DbSet<TrackingRecord> Records => Set<TrackingRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TrackingProject>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ProjectNo).HasColumnName("project_no").HasMaxLength(20);
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100);
            entity.Property(e => e.Type).HasColumnName("type").HasMaxLength(10);
            entity.Property(e => e.Client).HasColumnName("client").HasMaxLength(100);
            entity.Property(e => e.Budget).HasColumnName("budget").HasPrecision(15, 2);
            entity.Property(e => e.BidAmount).HasColumnName("bid_amount").HasPrecision(15, 2);
            entity.Property(e => e.RegisterDeadline).HasColumnName("register_deadline");
            entity.Property(e => e.BidDeadline).HasColumnName("bid_deadline");
            entity.Property(e => e.BidOpenDate).HasColumnName("bid_open_date");
            entity.Property(e => e.Location).HasColumnName("location").HasMaxLength(200);
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.FileStatus).HasColumnName("file_status").HasMaxLength(10);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(10);
            entity.Property(e => e.SuccessRate).HasColumnName("success_rate");
            entity.Property(e => e.Remark).HasColumnName("remark");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");

            entity.HasMany(e => e.Records)
                  .WithOne(r => r.Project)
                  .HasForeignKey(r => r.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TrackingRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.Timestamp).HasColumnName("timestamp");
            entity.Property(e => e.Type).HasColumnName("type").HasMaxLength(20);
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.Operator).HasColumnName("operator").HasMaxLength(50);
        });
    }
}
