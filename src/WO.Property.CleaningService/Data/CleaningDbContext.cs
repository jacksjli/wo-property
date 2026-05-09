using Microsoft.EntityFrameworkCore;
using WO.Property.CleaningService.Models;

namespace WO.Property.CleaningService.Data;

public class CleaningDbContext : DbContext
{
    public CleaningDbContext(DbContextOptions<CleaningDbContext> options) : base(options) { }

    public DbSet<CleaningStaff> Staff => Set<CleaningStaff>();
    public DbSet<CleaningTask> Tasks => Set<CleaningTask>();
    public DbSet<CleaningRecord> Records => Set<CleaningRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CleaningStaff>().HasData(
            new CleaningStaff { Id = 1, Name = "张保洁", Phone = "13800001111", Area = "东区", WorkShift = "上午", ProjectId = 1 },
            new CleaningStaff { Id = 2, Name = "李保洁", Phone = "13800002222", Area = "西区", WorkShift = "下午", ProjectId = 1 }
        );

        modelBuilder.Entity<CleaningTask>().HasData(
            new CleaningTask { Id = 1, StaffId = 1, Location = "东区1号楼", Content = "大厅、走廊、楼梯打扫", PlanDate = DateTime.Today, Status = "pending", ProjectId = 1 },
            new CleaningTask { Id = 2, StaffId = 2, Location = "西区中心花园", Content = "绿化带垃圾清理", PlanDate = DateTime.Today, Status = "in_progress", ProjectId = 1 }
        );
    }
}
