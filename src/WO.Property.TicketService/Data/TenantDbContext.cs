using Microsoft.EntityFrameworkCore;

namespace WO.Property.TicketService.Data;

/// <summary>
/// 支持动态切换租户库的 DbContext
/// </summary>
public class TenantDbContext : DbContext
{
    private readonly Tenant.ITenantDbFactory _tenantDbFactory;
    private readonly ILogger<TenantDbContext> _logger;

    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TimeoutRule> TimeoutRules => Set<TimeoutRule>();
    public DbSet<TimeoutAlert> TimeoutAlerts => Set<TimeoutAlert>();
    public DbSet<TicketProcessRecord> TicketProcessRecords => Set<TicketProcessRecord>();

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
        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("tickets");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TicketCode).HasColumnName("TicketNumber");
            entity.Property(e => e.Title).HasColumnName("Title");
            entity.Property(e => e.Description).HasColumnName("Description");
            entity.Property(e => e.Category).HasColumnName("category");
            entity.Property(e => e.Priority).HasColumnName("Priority");
            entity.Property(e => e.Status).HasColumnName("Status");
            entity.Property(e => e.CreatorPersonId).HasColumnName("creator_id");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.ProjectCode).HasColumnName("project_code");
            entity.Property(e => e.TicketTypeId).HasColumnName("ticket_type_id");
            entity.Property(e => e.AreaId).HasColumnName("area_id");
            entity.Property(e => e.BuildingId).HasColumnName("BuildingId");
            entity.Property(e => e.RoomId).HasColumnName("RoomId");
            entity.Property(e => e.ContactPersonName).HasColumnName("ContactPersonName");
            entity.Property(e => e.ContactPhone).HasColumnName("ContactPhone");
            entity.Property(e => e.Location).HasColumnName("Location");
            // entity.Property(e => e.CurrentRole).HasColumnName("current_role"); // 临时注释掉（数据库无此列）
            // entity.Property(e => e.EscalationLevel).HasColumnName("escalation_level"); // 临时注释掉
            entity.Property(e => e.LastEscalatedAt).HasColumnName("last_escalated_at");

            // 工单处理时间节点
            entity.Property(e => e.AssignedAt).HasColumnName("assigned_at");
            entity.Property(e => e.DispatchStatus).HasColumnName("dispatch_status");
            entity.Property(e => e.StartedAt).HasColumnName("started_at");
            entity.Property(e => e.FinishedAt).HasColumnName("finished_at");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");

            // 数据库自动生成的时间戳，不参与 INSERT/UPDATE
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt").ValueGeneratedOnUpdate();

            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Ignore(e => e.Images);
            entity.Ignore(e => e.CreatedBy);
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.IsDeleted);
            entity.Ignore(e => e.EscalationLevel);
            entity.Ignore(e => e.CurrentRole);
            entity.Ignore(e => e.LastEscalatedAt);
            entity.Property(e => e.AssigneePersonId).HasColumnName("assignee_id");
            entity.Property(e => e.JobTypeId).HasColumnName("jobTypeId");
        });

        modelBuilder.Entity<TimeoutRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("timeout_rules");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Color).HasColumnName("color");
            entity.Property(e => e.Role).HasColumnName("role");
            entity.Property(e => e.Hours).HasColumnName("hours");
            entity.Property(e => e.Enabled).HasColumnName("enabled");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<TicketProcessRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("ticket_process_records");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TicketId).HasColumnName("ticket_id");
            entity.Property(e => e.Action).HasColumnName("action");
            entity.Property(e => e.OperatorId).HasColumnName("operator_id");
            entity.Property(e => e.OperatorName).HasColumnName("operator_name");
            entity.Property(e => e.FromStatus).HasColumnName("from_status");
            entity.Property(e => e.ToStatus).HasColumnName("to_status");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
            entity.Ignore(e => e.CreatedBy);
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.IsDeleted);
            entity.Ignore(e => e.EscalationLevel);
            entity.Ignore(e => e.CurrentRole);
            entity.Ignore(e => e.LastEscalatedAt);
        });

        modelBuilder.Entity<TimeoutAlert>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("timeout_alerts");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TicketId).HasColumnName("ticket_id");
            entity.Property(e => e.TicketCode).HasColumnName("ticket_code");
            entity.Property(e => e.DispatchRecordId).HasColumnName("dispatch_record_id");
            entity.Property(e => e.AlertType).HasColumnName("alert_type");
            entity.Property(e => e.ExpectedTime).HasColumnName("expected_time");
            entity.Property(e => e.ActualTime).HasColumnName("actual_time");
            entity.Property(e => e.TimeoutMinutes).HasColumnName("timeout_minutes");
            entity.Property(e => e.Level).HasColumnName("level");
            entity.Property(e => e.NotifyTargetId).HasColumnName("notify_target_id");
            entity.Property(e => e.NotifyTargetName).HasColumnName("notify_target_name");
            entity.Property(e => e.NotificationId).HasColumnName("notification_id");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.SentAt).HasColumnName("sent_at");
            entity.Property(e => e.ProcessedAt).HasColumnName("processed_at");
            entity.Property(e => e.TenantCode).HasColumnName("tenant_code");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            // ProjectCode only exists on Ticket entity
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });
    }
}