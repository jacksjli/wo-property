using Microsoft.EntityFrameworkCore;
using WO.Property.DispatchService.Models;
using WO.Property.DispatchService.Tenant;

namespace WO.Property.DispatchService.Data;

/// <summary>
/// 租户 DbContext
/// </summary>
public class TenantDbContext : DbContext
{
    private readonly Tenant.ITenantDbFactory _tenantDbFactory;
    private readonly ILogger<TenantDbContext> _logger;

    public DbSet<DispatchRecord> DispatchRecords => Set<DispatchRecord>();
    public DbSet<TransferRequest> TransferRequests => Set<TransferRequest>();
    public DbSet<DispatchRule> DispatchRules => Set<DispatchRule>();
    public DbSet<PersonWorkload> PersonWorkloads => Set<PersonWorkload>();
    public DbSet<SatisfactionRating> SatisfactionRatings => Set<SatisfactionRating>();
    public DbSet<TimeoutAlert> TimeoutAlerts => Set<TimeoutAlert>();
    public DbSet<TimeoutRule> TimeoutRules => Set<TimeoutRule>();
    public DbSet<TimeoutEscalation> TimeoutEscalations => Set<TimeoutEscalation>();

    public TenantDbContext(
        DbContextOptions<TenantDbContext> options,
        Tenant.ITenantDbFactory tenantDbFactory,
        ILogger<TenantDbContext> logger)
        : base(options)
    {
        _tenantDbFactory = tenantDbFactory;
        _logger = logger;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var tenantCode = _tenantDbFactory.GetCurrentTenantCode();
            _logger.LogDebug("TenantDbContext configuring for tenant: {TenantCode}", tenantCode ?? "none");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // dispatch_records 表配置
        modelBuilder.Entity<DispatchRecord>(entity =>
        {
            entity.ToTable("dispatch_records");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TicketId).HasColumnName("ticket_id");
            entity.Property(e => e.TicketCode).HasColumnName("ticket_code").HasMaxLength(50);
            entity.Property(e => e.DispatchTime).HasColumnName("dispatch_time");
            entity.Property(e => e.FromPersonId).HasColumnName("from_person_id");
            entity.Property(e => e.FromPersonName).HasColumnName("from_person_name").HasMaxLength(50);
            entity.Property(e => e.ToPersonId).HasColumnName("to_person_id");
            entity.Property(e => e.ToPersonName).HasColumnName("to_person_name").HasMaxLength(50);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20);
            entity.Property(e => e.Source).HasColumnName("source").HasMaxLength(20);
            entity.Property(e => e.WorkflowInstanceId).HasColumnName("workflow_instance_id").HasMaxLength(50);
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.ConfirmedAt).HasColumnName("confirmed_at");
            entity.Property(e => e.ConfirmedBy).HasColumnName("confirmed_by");
            entity.Property(e => e.ConfirmedByName).HasColumnName("confirmed_by_name").HasMaxLength(50);
            entity.Property(e => e.AcceptedAt).HasColumnName("accepted_at");
            entity.Property(e => e.RatingId).HasColumnName("rating_id");
            entity.Property(e => e.SourceDispatchId).HasColumnName("source_dispatch_id");
            entity.Property(e => e.SourceType).HasColumnName("source_type").HasMaxLength(20);
            entity.Property(e => e.ParentDispatchId).HasColumnName("parent_dispatch_id");
            entity.Property(e => e.EscalationLevel).HasColumnName("escalation_level").HasMaxLength(20);
            entity.Property(e => e.TenantCode).HasColumnName("tenant_code").HasMaxLength(50);
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        // transfer_requests 表配置
        modelBuilder.Entity<TransferRequest>(entity =>
        {
            entity.ToTable("transfer_requests");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DispatchRecordId).HasColumnName("dispatch_record_id");
            entity.Property(e => e.TicketId).HasColumnName("ticket_id");
            entity.Property(e => e.TicketCode).HasColumnName("ticket_code").HasMaxLength(50);
            entity.Property(e => e.FromPersonId).HasColumnName("from_person_id");
            entity.Property(e => e.FromPersonName).HasColumnName("from_person_name").HasMaxLength(50);
            entity.Property(e => e.ToPersonId).HasColumnName("to_person_id");
            entity.Property(e => e.ToPersonName).HasColumnName("to_person_name").HasMaxLength(50);
            entity.Property(e => e.Reason).HasColumnName("reason").HasMaxLength(500);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20);
            entity.Property(e => e.ApprovedBy).HasColumnName("approved_by");
            entity.Property(e => e.ApprovedByName).HasColumnName("approved_by_name").HasMaxLength(50);
            entity.Property(e => e.ApprovedAt).HasColumnName("approved_at");
            entity.Property(e => e.ApprovedReason).HasColumnName("approved_reason").HasMaxLength(500);
            entity.Property(e => e.TransferDispatchId).HasColumnName("transfer_dispatch_id");
            entity.Property(e => e.TenantCode).HasColumnName("tenant_code").HasMaxLength(50);
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        // dispatch_rules 表配置
        modelBuilder.Entity<DispatchRule>(entity =>
        {
            entity.ToTable("dispatch_rules");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RuleName).HasColumnName("rule_name").HasMaxLength(100);
            entity.Property(e => e.Priority).HasColumnName("priority");
            entity.Property(e => e.TicketTypeId).HasColumnName("ticket_type_id");
            entity.Property(e => e.TicketTypeName).HasColumnName("ticket_type_name").HasMaxLength(50);
            entity.Property(e => e.AreaId).HasColumnName("area_id");
            entity.Property(e => e.AreaName).HasColumnName("area_name").HasMaxLength(50);
            entity.Property(e => e.PersonId).HasColumnName("person_id");
            entity.Property(e => e.PersonName).HasColumnName("person_name").HasMaxLength(50);
            entity.Property(e => e.BalanceStrategy).HasColumnName("balance_strategy").HasMaxLength(20);
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.TenantCode).HasColumnName("tenant_code").HasMaxLength(50);
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        // person_workload 表配置
        modelBuilder.Entity<PersonWorkload>(entity =>
        {
            entity.ToTable("person_workload");
            entity.HasKey(e => e.PersonId);
            entity.Property(e => e.PersonId).HasColumnName("person_id");
            entity.Property(e => e.PersonName).HasColumnName("person_name").HasMaxLength(50);
            entity.Property(e => e.ActiveTicketCount).HasColumnName("active_ticket_count");
            entity.Property(e => e.TotalDispatched).HasColumnName("total_dispatched");
            entity.Property(e => e.TotalCompleted).HasColumnName("total_completed");
            entity.Property(e => e.LastDispatchTime).HasColumnName("last_dispatch_time");
            entity.Property(e => e.TenantCode).HasColumnName("tenant_code").HasMaxLength(50);
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        // satisfaction_ratings 表配置
        modelBuilder.Entity<SatisfactionRating>(entity =>
        {
            entity.ToTable("satisfaction_ratings");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TicketId).HasColumnName("ticket_id");
            entity.Property(e => e.TicketCode).HasColumnName("ticket_code").HasMaxLength(50);
            entity.Property(e => e.DispatchRecordId).HasColumnName("dispatch_record_id");
            entity.Property(e => e.RaterId).HasColumnName("rater_id");
            entity.Property(e => e.RaterName).HasColumnName("rater_name").HasMaxLength(50);
            entity.Property(e => e.RateeId).HasColumnName("ratee_id");
            entity.Property(e => e.RateeName).HasColumnName("ratee_name").HasMaxLength(50);
            entity.Property(e => e.QualityScore).HasColumnName("quality_score");
            entity.Property(e => e.AttitudeScore).HasColumnName("attitude_score");
            entity.Property(e => e.TimelinessScore).HasColumnName("timeliness_score");
            entity.Property(e => e.OverallScore).HasColumnName("overall_score");
            entity.Property(e => e.Comment).HasColumnName("comment").HasMaxLength(500);
            entity.Property(e => e.Images).HasColumnName("images").HasMaxLength(1000);
            entity.Property(e => e.RatedAt).HasColumnName("rated_at");
            entity.Property(e => e.IsAutoRated).HasColumnName("is_auto_rated");
            entity.Property(e => e.TenantCode).HasColumnName("tenant_code").HasMaxLength(50);
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        // timeout_alerts 表配置
        modelBuilder.Entity<TimeoutAlert>(entity =>
        {
            entity.ToTable("timeout_alerts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TicketId).HasColumnName("ticket_id");
            entity.Property(e => e.TicketCode).HasColumnName("ticket_code").HasMaxLength(50);
            entity.Property(e => e.DispatchRecordId).HasColumnName("dispatch_record_id");
            entity.Property(e => e.AlertType).HasColumnName("alert_type").HasMaxLength(30);
            entity.Property(e => e.ExpectedTime).HasColumnName("expected_time");
            entity.Property(e => e.ActualTime).HasColumnName("actual_time");
            entity.Property(e => e.TimeoutMinutes).HasColumnName("timeout_minutes");
            entity.Property(e => e.Level).HasColumnName("level");
            entity.Property(e => e.NotifyTargetId).HasColumnName("notify_target_id");
            entity.Property(e => e.NotifyTargetName).HasColumnName("notify_target_name").HasMaxLength(50);
            entity.Property(e => e.NotificationId).HasColumnName("notification_id");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20);
            entity.Property(e => e.SentAt).HasColumnName("sent_at");
            entity.Property(e => e.ProcessedAt).HasColumnName("processed_at");
            entity.Property(e => e.TenantCode).HasColumnName("tenant_code").HasMaxLength(50);
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });

        // timeout_rules 表配置
        modelBuilder.Entity<TimeoutRule>(entity =>
        {
            entity.ToTable("timeout_rules");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Color).HasColumnName("color").HasMaxLength(20);
            entity.Property(e => e.Role).HasColumnName("role").HasMaxLength(50);
            entity.Property(e => e.Hours).HasColumnName("hours");
            entity.Property(e => e.Enabled).HasColumnName("enabled");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        // timeout_escalations 表配置
        modelBuilder.Entity<TimeoutEscalation>(entity =>
        {
            entity.ToTable("timeout_escalations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TicketId).HasColumnName("ticket_id");
            entity.Property(e => e.TicketCode).HasColumnName("ticket_code").HasMaxLength(50);
            entity.Property(e => e.DispatchRecordId).HasColumnName("dispatch_record_id");
            entity.Property(e => e.FromPersonId).HasColumnName("from_person_id");
            entity.Property(e => e.FromPersonName).HasColumnName("from_person_name").HasMaxLength(50);
            entity.Property(e => e.ToPersonId).HasColumnName("to_person_id");
            entity.Property(e => e.ToPersonName).HasColumnName("to_person_name").HasMaxLength(50);
            entity.Property(e => e.ToRole).HasColumnName("to_role").HasMaxLength(50);
            entity.Property(e => e.Level).HasColumnName("level");
            entity.Property(e => e.EscalatedAt).HasColumnName("escalated_at");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20);
            entity.Property(e => e.TenantCode).HasColumnName("tenant_code").HasMaxLength(50);
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });
    }
}