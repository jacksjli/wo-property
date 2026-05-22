using Microsoft.EntityFrameworkCore;
using WO.Property.NotificationService.Models;

namespace WO.Property.NotificationService.Data;

public class TenantDbContext : DbContext
{
    private readonly Tenant.ITenantDbFactory _tenantDbFactory;
    private readonly ILogger<TenantDbContext> _logger;

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<MessageTemplate> MessageTemplates => Set<MessageTemplate>();

    public TenantDbContext(DbContextOptions<TenantDbContext> options, Tenant.ITenantDbFactory tenantDbFactory, ILogger<TenantDbContext> logger)
        : base(options)
    {
        _tenantDbFactory = tenantDbFactory;
        _logger = logger;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("notifications");
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.UserId).HasColumnName("UserId");
            entity.Property(e => e.Title).HasColumnName("Title");
            entity.Property(e => e.Content).HasColumnName("Content");
            entity.Property(e => e.Type).HasColumnName("Type");
            entity.Property(e => e.Priority).HasColumnName("Priority");
            entity.Property(e => e.IsRead).HasColumnName("IsRead");
            entity.Property(e => e.ReadAt).HasColumnName("ReadAt");
            entity.Property(e => e.RelatedEntityType).HasColumnName("RelatedEntityType");
            entity.Property(e => e.RelatedEntityId).HasColumnName("RelatedEntityId");
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt");
        });

        modelBuilder.Entity<MessageTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("MessageTemplates");
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.Name).HasColumnName("Name");
            entity.Property(e => e.Type).HasColumnName("Type");
            entity.Property(e => e.Subject).HasColumnName("Subject");
            entity.Property(e => e.Content).HasColumnName("Content");
            entity.Property(e => e.Variables).HasColumnName("Variables");
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt");
        });
    }
}
