using Microsoft.EntityFrameworkCore;
using WO.Property.MobileService.Models;

namespace WO.Property.MobileService.Data;

public class MobileDbContext : DbContext
{
    public MobileDbContext(DbContextOptions<MobileDbContext> options) : base(options)
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
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DeviceId).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.Property(e => e.DeviceType).HasConversion<string>();
        });
        
        // PushNotification 配置
        modelBuilder.Entity<PushNotification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.NotificationId).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);
            entity.Property(e => e.Type).HasConversion<string>();
        });
        
        // UserSession 配置
        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SessionId).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ExpiresAt);
        });
        
        // WeChatUser 配置
        modelBuilder.Entity<WeChatUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OpenId).IsUnique();
            entity.HasIndex(e => e.UnionId);
        });
        
        // QuickEntry 配置
        modelBuilder.Entity<QuickEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.SortOrder);
        });
        
        // 种子数据
        SeedData(modelBuilder);
    }
    
    private void SeedData(ModelBuilder modelBuilder)
    {
        var now = DateTime.UtcNow;
        
        // 快捷入口
        modelBuilder.Entity<QuickEntry>().HasData(
            new QuickEntry { Id = 1, Code = "payment", Name = "物业缴费", Description = "在线缴纳物业费", Icon = "icon-payment", TargetUrl = "/pages/payment/index", SortOrder = 1, Category = "payment", IsActive = true, CreatedAt = now },
            new QuickEntry { Id = 2, Code = "ticket", Name = "我要报修", Description = "提交维修工单", Icon = "icon-repair", TargetUrl = "/pages/ticket/create", SortOrder = 2, Category = "ticket", IsActive = true, CreatedAt = now },
            new QuickEntry { Id = 3, Code = "complaint", Name = "投诉建议", Description = "提交投诉或建议", Icon = "icon-complaint", TargetUrl = "/pages/complaint/create", SortOrder = 3, Category = "complaint", IsActive = true, CreatedAt = now },
            new QuickEntry { Id = 4, Code = "visitor", Name = "访客邀请", Description = "预约访客通行", Icon = "icon-visitor", TargetUrl = "/pages/visitor/create", SortOrder = 4, Category = "visitor", IsActive = true, CreatedAt = now },
            new QuickEntry { Id = 5, Code = "key", Name = "钥匙借用", Description = "借用公共钥匙", Icon = "icon-key", TargetUrl = "/pages/key/index", SortOrder = 5, Category = "key", IsActive = true, CreatedAt = now },
            new QuickEntry { Id = 6, Code = "notice", Name = "小区公告", Description = "查看小区公告", Icon = "icon-notice", TargetUrl = "/pages/notice/index", SortOrder = 6, Category = "notice", IsActive = true, CreatedAt = now },
            new QuickEntry { Id = 7, Code = "inspection", Name = "巡检任务", Description = "查看巡检任务", Icon = "icon-inspection", TargetUrl = "/pages/inspection/index", SortOrder = 7, Category = "inspection", IsActive = true, CreatedAt = now },
            new QuickEntry { Id = 8, Code = "profile", Name = "个人信息", Description = "个人设置", Icon = "icon-profile", TargetUrl = "/pages/profile/index", SortOrder = 8, Category = "profile", IsActive = true, CreatedAt = now }
        );
        
        // 示例设备注册
        modelBuilder.Entity<DeviceRegistration>().HasData(
            new DeviceRegistration
            {
                Id = 1,
                DeviceId = "DEV-ANDROID-001",
                DeviceType = DeviceType.Android,
                DeviceName = "小米12",
                DeviceModel = "Xiaomi 12",
                OsVersion = "Android 13",
                AppVersion = "1.0.0",
                UserId = "admin",
                NotificationsEnabled = true,
                LastActiveAt = now,
                CreatedAt = now,
                UpdatedAt = now
            },
            new DeviceRegistration
            {
                Id = 2,
                DeviceId = "DEV-IPHONE-001",
                DeviceType = DeviceType.iOS,
                DeviceName = "iPhone 14",
                DeviceModel = "iPhone 14 Pro",
                OsVersion = "iOS 17",
                AppVersion = "1.0.0",
                UserId = "tech",
                NotificationsEnabled = true,
                LastActiveAt = now,
                CreatedAt = now,
                UpdatedAt = now
            }
        );
        
        // 示例推送通知
        modelBuilder.Entity<PushNotification>().HasData(
            new PushNotification
            {
                Id = 1,
                NotificationId = "NOT-2026-0001",
                UserId = "admin",
                Type = NotificationType.Announcement,
                Title = "小区公告",
                Content = "明天下午2点进行消防演练，请各位业主配合。",
                IsSent = true,
                SentAt = now.AddHours(-2),
                CreatedAt = now.AddHours(-2)
            },
            new PushNotification
            {
                Id = 2,
                NotificationId = "NOT-2026-0002",
                UserId = "tech",
                Type = NotificationType.Ticket,
                Title = "工单提醒",
                Content = "您有一个新的维修工单待处理。",
                IsSent = true,
                SentAt = now.AddHours(-1),
                CreatedAt = now.AddHours(-1)
            }
        );
    }
}
