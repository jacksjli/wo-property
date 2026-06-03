using Microsoft.EntityFrameworkCore;
using WO.Property.AnnouncementService.Models;

namespace WO.Property.AnnouncementService.Data;

public class AnnouncementDbContext : DbContext
{
    public AnnouncementDbContext(DbContextOptions<AnnouncementDbContext> options) : base(options) { }

    public DbSet<Announcement> Announcements { get; set; }
    public DbSet<AnnouncementRead> AnnouncementReads { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Announcement>(e => {
            e.HasIndex(a => a.Status);
            e.HasIndex(a => a.Category);
            e.HasIndex(a => a.ProjectId);
            e.HasIndex(a => a.ProjectCode);
            e.HasIndex(a => a.ProjectCode);
            e.Property(a => a.ProjectCode).HasMaxLength(20);
        });

        modelBuilder.Entity<AnnouncementRead>(e => {
            e.HasIndex(r => new { r.AnnouncementId, r.UserId }).IsUnique();
        });

        var now = DateTime.UtcNow;
        modelBuilder.Entity<Announcement>().HasData(
            new Announcement { Id = 1, Title = "关于五一假期安全的温馨提示", Content = "五一假期将至，请各位业主注意防火防盗，外出时关好门窗。祝大家假期愉快！", Category = "security", Level = "info", Status = "published", IsPinned = true, Publisher = "物业管理处", PublishTime = now.AddDays(-1), ProjectId = 1, CreatedAt = now.AddDays(-1), UpdatedAt = now },
            new Announcement { Id = 2, Title = "A区电梯维保通知", Content = "A区电梯将于5月3日进行例行维保，届时电梯将暂停使用2小时，请各位业主提前安排出行。", Category = "maintenance", Level = "warning", Status = "published", Publisher = "工程部", PublishTime = now.AddHours(-5), ProjectId = 1, CreatedAt = now.AddHours(-5), UpdatedAt = now }
        );
    }
}