using Microsoft.EntityFrameworkCore;
using WO.Property.CommunityService.Models;

namespace WO.Property.CommunityService.Data;

public class CommunityDbContext : DbContext
{
    public CommunityDbContext(DbContextOptions<CommunityDbContext> options) : base(options) { }

    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<ActivityEnrollment> Enrollments => Set<ActivityEnrollment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Activity>().HasData(
            new Activity { Id = 1, Name = "端午节包粽子活动", Location = "小区广场", StartTime = DateTime.UtcNow.AddDays(7), EndTime = DateTime.UtcNow.AddDays(7).AddHours(4), MaxParticipants = 50, CurrentParticipants = 32, Status = "open", Description = "端午节传统文化活动，欢迎大家参加", ProjectId = 1 },
            new Activity { Id = 2, Name = "免费义诊活动", Location = "物业管理处", StartTime = DateTime.UtcNow.AddDays(14), EndTime = DateTime.UtcNow.AddDays(14).AddHours(6), MaxParticipants = 30, CurrentParticipants = 15, Status = "open", Description = "邀请专业医师为业主提供免费健康咨询", ProjectId = 1 }
        );
    }
}
