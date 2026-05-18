using Microsoft.EntityFrameworkCore;
using WO.Property.RenovationService.Models;

namespace WO.Property.RenovationService.Data;

public class TenantDbContext : DbContext
{
    private readonly Tenant.ITenantDbFactory _tenantDbFactory;
    private readonly ILogger<TenantDbContext> _logger;

    public DbSet<RenovationApplication> RenovationApplications => Set<RenovationApplication>();

    public TenantDbContext(DbContextOptions<TenantDbContext> options, Tenant.ITenantDbFactory tenantDbFactory, ILogger<TenantDbContext> logger)
        : base(options)
    {
        _tenantDbFactory = tenantDbFactory;
        _logger = logger;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RenovationApplication>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("renovation_applications");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ApplicantName).HasColumnName("applicant_name");
            entity.Property(e => e.ApplicantPhone).HasColumnName("applicant_phone");
            entity.Property(e => e.Building).HasColumnName("building");
            entity.Property(e => e.Unit).HasColumnName("unit");
            entity.Property(e => e.RoomNo).HasColumnName("room_no");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.AttachmentUrls).HasColumnName("attachment_urls");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.DepositAmount).HasColumnName("deposit_amount");
            entity.Property(e => e.Remark).HasColumnName("remark");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.UpdatedAt);
            entity.Ignore(e => e.IsDeleted);
        });
    }
}