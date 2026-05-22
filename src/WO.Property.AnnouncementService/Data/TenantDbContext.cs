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
    public DbSet<DeviceReport> DeviceReports => Set<DeviceReport>();
    public DbSet<TicketReport> TicketReports => Set<TicketReport>();
    public DbSet<MaterialReport> MaterialReports => Set<MaterialReport>();
    public DbSet<SatisfactionSurvey> SatisfactionSurveys => Set<SatisfactionSurvey>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();
    public DbSet<EnumDefinition> EnumDefinitions => Set<EnumDefinition>();
    public DbSet<GeneralReport> GeneralReports => Set<GeneralReport>();

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

        modelBuilder.Entity<DeviceReport>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("DeviceReports");
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.Code).HasColumnName("Code");
            entity.Property(e => e.Name).HasColumnName("Name");
            entity.Property(e => e.Status).HasColumnName("Status");
            entity.Property(e => e.MaintenanceType).HasColumnName("MaintenanceType");
            entity.Property(e => e.MaintenanceCost).HasColumnName("MaintenanceCost");
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt");
        });

        modelBuilder.Entity<TicketReport>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("TicketReports");
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.TicketNumber).HasColumnName("TicketNumber");
            entity.Property(e => e.Title).HasColumnName("Title");
            entity.Property(e => e.Status).HasColumnName("Status");
            entity.Property(e => e.Priority).HasColumnName("Priority");
            entity.Property(e => e.AssignedTo).HasColumnName("AssignedTo");
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt");
            entity.Property(e => e.ResolvedAt).HasColumnName("ResolvedAt");
        });

        modelBuilder.Entity<MaterialReport>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("MaterialReports");
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.Code).HasColumnName("Code");
            entity.Property(e => e.Name).HasColumnName("Name");
            entity.Property(e => e.CurrentStock).HasColumnName("CurrentStock");
            entity.Property(e => e.SafetyStock).HasColumnName("SafetyStock");
            entity.Property(e => e.UnitPrice).HasColumnName("UnitPrice");
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt");
        });

        modelBuilder.Entity<SatisfactionSurvey>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("satisfaction_surveys");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TicketId).HasColumnName("ticket_id");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.Comment).HasColumnName("comment");
            entity.Property(e => e.RespondentName).HasColumnName("respondent_name");
            entity.Property(e => e.SubmittedAt).HasColumnName("submitted_at");
        });

        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("PurchaseOrders");
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.OrderNumber).HasColumnName("OrderNumber");
            entity.Property(e => e.OrderDate).HasColumnName("OrderDate");
            entity.Property(e => e.Supplier).HasColumnName("Supplier");
            entity.Property(e => e.TotalAmount).HasColumnName("TotalAmount");
            entity.Property(e => e.Status).HasColumnName("Status");
            entity.Property(e => e.Notes).HasColumnName("Notes");
            entity.Property(e => e.CreatedBy).HasColumnName("CreatedBy");
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt");
            entity.Property(e => e.UpdatedBy).HasColumnName("UpdatedBy");
            entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt");
            entity.Property(e => e.IsDeleted).HasColumnName("IsDeleted");
        });

        modelBuilder.Entity<StockTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("StockTransactions");
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.MaterialId).HasColumnName("MaterialId");
            entity.Property(e => e.TransactionType).HasColumnName("TransactionType");
            entity.Property(e => e.Quantity).HasColumnName("Quantity");
            entity.Property(e => e.UnitPrice).HasColumnName("UnitPrice");
            entity.Property(e => e.TotalAmount).HasColumnName("TotalAmount");
            entity.Property(e => e.Operator).HasColumnName("Operator");
            entity.Property(e => e.Notes).HasColumnName("Notes");
            entity.Property(e => e.CreatedBy).HasColumnName("CreatedBy");
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt");
            entity.Property(e => e.UpdatedBy).HasColumnName("UpdatedBy");
            entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt");
            entity.Property(e => e.IsDeleted).HasColumnName("IsDeleted");
        });

        modelBuilder.Entity<EnumDefinition>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("EnumDefinitions");
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.Category).HasColumnName("Category");
            entity.Property(e => e.Code).HasColumnName("Code");
            entity.Property(e => e.Name).HasColumnName("Name");
            entity.Property(e => e.Description).HasColumnName("Description");
            entity.Property(e => e.SortOrder).HasColumnName("SortOrder");
            entity.Property(e => e.IsActive).HasColumnName("IsActive");
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt");
        });

        modelBuilder.Entity<GeneralReport>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Reports");
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.ReportNumber).HasColumnName("ReportNumber");
            entity.Property(e => e.Title).HasColumnName("Title");
            entity.Property(e => e.Type).HasColumnName("Type");
            entity.Property(e => e.Category).HasColumnName("Category");
            entity.Property(e => e.StartDate).HasColumnName("StartDate");
            entity.Property(e => e.EndDate).HasColumnName("EndDate");
            entity.Property(e => e.Data).HasColumnName("Data");
            entity.Property(e => e.Summary).HasColumnName("Summary");
            entity.Property(e => e.GeneratedBy).HasColumnName("GeneratedBy");
            entity.Property(e => e.GeneratedAt).HasColumnName("GeneratedAt");
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt");
        });
    }
}