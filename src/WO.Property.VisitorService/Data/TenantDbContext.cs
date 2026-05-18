using Microsoft.EntityFrameworkCore;
using WO.Property.VisitorService.Models;

namespace WO.Property.VisitorService.Data;

public class TenantDbContext : DbContext
{
    private readonly Tenant.ITenantDbFactory _tenantDbFactory;
    private readonly ILogger<TenantDbContext> _logger;

    public DbSet<Visitor> Visitors => Set<Visitor>();
    public DbSet<VisitRecord> VisitRecords => Set<VisitRecord>();

    public TenantDbContext(DbContextOptions<TenantDbContext> options, Tenant.ITenantDbFactory tenantDbFactory, ILogger<TenantDbContext> logger)
        : base(options)
    {
        _tenantDbFactory = tenantDbFactory;
        _logger = logger;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Visitor>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("visitors");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.VisitorNumber).HasColumnName("visitor_number");
            entity.Property(e => e.VisitorName).HasColumnName("visitor_name");
            entity.Property(e => e.VisitorPhone).HasColumnName("visitor_phone");
            entity.Property(e => e.VisitorEmail).HasColumnName("visitor_email");
            entity.Property(e => e.IDType).HasColumnName("id_type").HasConversion<string?>();
            entity.Property(e => e.IDNumber).HasColumnName("id_number");
            entity.Property(e => e.Type).HasColumnName("type").HasConversion<string>();
            entity.Property(e => e.HostName).HasColumnName("host_name");
            entity.Property(e => e.HostPhone).HasColumnName("host_phone");
            entity.Property(e => e.HostUnit).HasColumnName("host_unit");
            entity.Property(e => e.VisitLocation).HasColumnName("visit_location");
            entity.Property(e => e.ScheduledDate).HasColumnName("scheduled_date");
            entity.Property(e => e.ScheduledStartTime).HasColumnName("scheduled_start_time");
            entity.Property(e => e.ScheduledEndTime).HasColumnName("scheduled_end_time");
            entity.Property(e => e.Purpose).HasColumnName("purpose");
            entity.Property(e => e.ExpectedVisitors).HasColumnName("expected_visitors");
            entity.Property(e => e.ActualCheckInTime).HasColumnName("actual_check_in_time");
            entity.Property(e => e.ActualCheckOutTime).HasColumnName("actual_check_out_time");
            entity.Property(e => e.Status).HasColumnName("status").HasConversion<string>();
            entity.Property(e => e.LicensePlate).HasColumnName("license_plate");
            entity.Property(e => e.ParkingSpace).HasColumnName("parking_space");
            entity.Property(e => e.Approver).HasColumnName("approver");
            entity.Property(e => e.ApprovedDate).HasColumnName("approved_date");
            entity.Property(e => e.ApprovalRemarks).HasColumnName("approval_remarks");
            entity.Property(e => e.AccessCode).HasColumnName("access_code");
            entity.Property(e => e.AccessGranted).HasColumnName("access_granted");
            entity.Property(e => e.Remarks).HasColumnName("remarks");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.UpdatedAt);
            entity.Ignore(e => e.IsDeleted);
        });

        modelBuilder.Entity<VisitRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("visit_records");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.VisitorId).HasColumnName("visitor_id");
            entity.Property(e => e.RecordNumber).HasColumnName("record_number");
            entity.Property(e => e.GateDevice).HasColumnName("gate_device");
            entity.Property(e => e.AccessDirection).HasColumnName("access_direction");
            entity.Property(e => e.AccessTime).HasColumnName("access_time");
            entity.Property(e => e.PhotoUrl).HasColumnName("photo_url");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.UpdatedAt);
            entity.Ignore(e => e.IsDeleted);
        });
    }
}