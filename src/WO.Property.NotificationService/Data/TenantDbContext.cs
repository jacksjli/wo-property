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
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.Priority).HasColumnName("priority");
            entity.Property(e => e.IsRead).HasColumnName("is_read");
            entity.Property(e => e.ReadAt).HasColumnName("read_at");
            entity.Property(e => e.RelatedEntityType).HasColumnName("related_entity_type");
            entity.Property(e => e.RelatedEntityId).HasColumnName("related_entity_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<MessageTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("message_templates");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.Subject).HasColumnName("subject");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.Variables).HasColumnName("variables");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });
    }
}
