using Microsoft.EntityFrameworkCore;
using WO.Property.MobileService.Models;
using WO.Property.Shared.Models;

namespace WO.Property.MobileService.Data;

/// <summary>
/// 租户数据库上下文 - 用于多租户隔离
/// </summary>
public class TenantDbContext : DbContext
{
    public TenantDbContext(DbContextOptions<TenantDbContext> options) : base(options)
    {
    }

    public DbSet<DeviceRegistration> Devices { get; set; }
    public DbSet<PushNotification> Notifications { get; set; }
    public DbSet<UserSession> Sessions { get; set; }
    public DbSet<WeChatUser> WeChatUsers { get; set; }
    public DbSet<QuickEntry> QuickEntries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // DeviceRegistration 配置
        modelBuilder.Entity<DeviceRegistration>(entity =>
        {
            entity.ToTable("mobile_device_registrations");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DeviceId).IsUnique().HasDatabaseName("idx_device_id");
            entity.HasIndex(e => e.UserId).HasDatabaseName("idx_user_id");
            entity.Property(e => e.DeviceType).HasConversion<string>().HasColumnType("varchar(50)");
            entity.Ignore(e => e.IsDeleted);
            entity.Property(e => e.ProjectCode).HasColumnName("project_code");
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.UpdatedAt);
        });

        // PushNotification 配置
        modelBuilder.Entity<PushNotification>(entity =>
        {
            entity.ToTable("mobile_push_notifications");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.NotificationId).IsUnique().HasDatabaseName("idx_notification_id");
            entity.HasIndex(e => e.UserId).HasDatabaseName("idx_notification_user_id");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("idx_created_at");
            entity.Property(e => e.Type).HasConversion<string>().HasColumnType("varchar(50)");
            entity.Ignore(e => e.IsDeleted);
            entity.Property(e => e.ProjectCode).HasColumnName("project_code");
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.UpdatedAt);
        });

        // UserSession 配置
        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.ToTable("mobile_user_sessions");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SessionId).IsUnique().HasDatabaseName("idx_session_id");
            entity.HasIndex(e => e.UserId).HasDatabaseName("idx_session_user_id");
            entity.HasIndex(e => e.ExpiresAt).HasDatabaseName("idx_expires_at");
            entity.Ignore(e => e.IsDeleted);
            entity.Property(e => e.ProjectCode).HasColumnName("project_code");
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.UpdatedAt);
        });

        // WeChatUser 配置
        modelBuilder.Entity<WeChatUser>(entity =>
        {
            entity.ToTable("mobile_wechat_users");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OpenId).IsUnique().HasDatabaseName("idx_open_id");
            entity.HasIndex(e => e.UnionId).HasDatabaseName("idx_union_id");
            entity.Ignore(e => e.IsDeleted);
            entity.Property(e => e.ProjectCode).HasColumnName("project_code");
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.UpdatedAt);
        });

        // QuickEntry 配置
        modelBuilder.Entity<QuickEntry>(entity =>
        {
            entity.ToTable("mobile_quick_entries");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique().HasDatabaseName("idx_entry_code");
            entity.HasIndex(e => e.Category).HasDatabaseName("idx_category");
            entity.HasIndex(e => e.SortOrder).HasDatabaseName("idx_sort_order");
            entity.Ignore(e => e.IsDeleted);
            entity.Property(e => e.ProjectCode).HasColumnName("project_code");
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.UpdatedAt);
        });
    }
}