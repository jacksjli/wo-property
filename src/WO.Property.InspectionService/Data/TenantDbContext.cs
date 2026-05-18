using Microsoft.EntityFrameworkCore;
using WO.Property.InspectionService.Models;

namespace WO.Property.InspectionService.Data;

public class TenantDbContext : DbContext
{
    private readonly Tenant.ITenantDbFactory _tenantDbFactory;
    private readonly ILogger<TenantDbContext> _logger;

    public DbSet<InspectionPlan> InspectionPlans => Set<InspectionPlan>();
    public DbSet<InspectionTask> InspectionTasks => Set<InspectionTask>();
    public DbSet<InspectionRecord> InspectionRecords => Set<InspectionRecord>();
    public DbSet<InspectionIssue> InspectionIssues => Set<InspectionIssue>();

    public TenantDbContext(DbContextOptions<TenantDbContext> options, Tenant.ITenantDbFactory tenantDbFactory, ILogger<TenantDbContext> logger)
        : base(options)
    {
        _tenantDbFactory = tenantDbFactory;
        _logger = logger;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InspectionPlan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("inspection_plans");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Type).HasColumnName("type").HasConversion<string>();
            entity.Property(e => e.Area).HasColumnName("area");
            entity.Property(e => e.TargetItems).HasColumnName("target_items");
            entity.Property(e => e.IntervalDays).HasColumnName("interval_days");
            entity.Property(e => e.NextExecutionDate).HasColumnName("next_execution_date");
            entity.Property(e => e.AssignedTo).HasColumnName("assigned_to");
            entity.Property(e => e.EstimatedMinutes).HasColumnName("estimated_minutes");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.UpdatedAt);
            entity.Ignore(e => e.IsDeleted);
        });

        modelBuilder.Entity<InspectionTask>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("inspection_tasks");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TaskNumber).HasColumnName("task_number");
            entity.Property(e => e.PlanId).HasColumnName("plan_id");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Type).HasColumnName("type").HasConversion<string>();
            entity.Property(e => e.Area).HasColumnName("area");
            entity.Property(e => e.ScheduledDate).HasColumnName("scheduled_date");
            entity.Property(e => e.StartTime).HasColumnName("start_time");
            entity.Property(e => e.EndTime).HasColumnName("end_time");
            entity.Property(e => e.Status).HasColumnName("status").HasConversion<string>();
            entity.Property(e => e.AssignedTo).HasColumnName("assigned_to");
            entity.Property(e => e.CompletedBy).HasColumnName("completed_by");
            entity.Property(e => e.Remarks).HasColumnName("remarks");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.UpdatedAt);
            entity.Ignore(e => e.IsDeleted);
            entity.Ignore(e => e.Plan);
        });

        modelBuilder.Entity<InspectionRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("inspection_records");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TaskId).HasColumnName("task_id");
            entity.Property(e => e.RecordNumber).HasColumnName("record_number");
            entity.Property(e => e.InspectionDate).HasColumnName("inspection_date");
            entity.Property(e => e.StartTime).HasColumnName("start_time");
            entity.Property(e => e.EndTime).HasColumnName("end_time");
            entity.Property(e => e.Inspector).HasColumnName("inspector");
            entity.Property(e => e.TotalItems).HasColumnName("total_items");
            entity.Property(e => e.PassedItems).HasColumnName("passed_items");
            entity.Property(e => e.FailedItems).HasColumnName("failed_items");
            entity.Property(e => e.Findings).HasColumnName("findings");
            entity.Property(e => e.Suggestions).HasColumnName("suggestions");
            entity.Property(e => e.IsPassed).HasColumnName("is_passed");
            entity.Property(e => e.Attachments).HasColumnName("attachments");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.UpdatedAt);
            entity.Ignore(e => e.IsDeleted);
            entity.Ignore(e => e.Task);
        });

        modelBuilder.Entity<InspectionIssue>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("inspection_issues");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IssueNumber).HasColumnName("issue_number");
            entity.Property(e => e.RecordId).HasColumnName("record_id");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Severity).HasColumnName("severity").HasConversion<string>();
            entity.Property(e => e.Status).HasColumnName("status").HasConversion<string>();
            entity.Property(e => e.Location).HasColumnName("location");
            entity.Property(e => e.Category).HasColumnName("category");
            entity.Property(e => e.Solution).HasColumnName("solution");
            entity.Property(e => e.AssignedTo).HasColumnName("assigned_to");
            entity.Property(e => e.DueDate).HasColumnName("due_date");
            entity.Property(e => e.ResolvedDate).HasColumnName("resolved_date");
            entity.Property(e => e.ResolvedBy).HasColumnName("resolved_by");
            entity.Property(e => e.Remarks).HasColumnName("remarks");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.UpdatedAt);
            entity.Ignore(e => e.IsDeleted);
            entity.Ignore(e => e.Record);
        });
    }
}