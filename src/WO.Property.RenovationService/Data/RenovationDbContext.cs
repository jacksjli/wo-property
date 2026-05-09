using Microsoft.EntityFrameworkCore;
using WO.Property.RenovationService.Models;

namespace WO.Property.RenovationService.Data;

public class RenovationDbContext : DbContext
{
    public RenovationDbContext(DbContextOptions<RenovationDbContext> options) : base(options) { }
    public DbSet<RenovationApplication> Applications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RenovationApplication>(e => { e.HasIndex(a => a.Status); e.HasIndex(a => a.ProjectId); });
        var now = DateTime.UtcNow;
        modelBuilder.Entity<RenovationApplication>().HasData(
            new RenovationApplication { Id = 1, ApplicantName = "张三", ApplicantPhone = "13800138001", Building = "A栋", RoomNo = "501", Content = "厨房整体装修，更换橱柜和瓷砖", StartDate = now.AddDays(5), EndDate = now.AddDays(30), Status = "pending", DepositAmount = 5000, ProjectId = 1, CreatedAt = now },
            new RenovationApplication { Id = 2, ApplicantName = "李四", ApplicantPhone = "13800138002", Building = "B栋", RoomNo = "1203", Content = "卫生间翻新，更换洁具和防水处理", StartDate = now.AddDays(2), EndDate = now.AddDays(20), Status = "approved", DepositAmount = 5000, ProjectId = 1, CreatedAt = now.AddDays(-2) }
        );
    }
}