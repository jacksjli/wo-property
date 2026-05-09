using Microsoft.EntityFrameworkCore;
using WO.Property.AccessControlService.Models;

namespace WO.Property.AccessControlService.Data;

public class AccessDbContext : DbContext
{
    public AccessDbContext(DbContextOptions<AccessDbContext> options) : base(options) { }

    public DbSet<AccessCard> AccessCards { get; set; }
    public DbSet<AccessDoor> AccessDoors { get; set; }
    public DbSet<TempAccessCode> TempAccessCodes { get; set; }
    public DbSet<AccessLog> AccessLogs { get; set; }
    public DbSet<Building> Buildings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccessCard>(e => {
            e.HasIndex(c => c.CardNo).IsUnique();
            e.HasIndex(c => c.Status);
        });
        modelBuilder.Entity<AccessDoor>(e => { e.HasIndex(d => d.BuildingId); });
        modelBuilder.Entity<TempAccessCode>(e => {
            e.HasIndex(c => c.Code);
            e.HasIndex(c => c.Status);
        });
        modelBuilder.Entity<AccessLog>(e => {
            e.HasIndex(l => l.AccessTime);
            e.HasIndex(l => l.BuildingId);
        });

        var now = DateTime.UtcNow;
        modelBuilder.Entity<Building>().HasData(
            new Building { Id = 1, Name = "A栋", ProjectId = 1, CreatedAt = now },
            new Building { Id = 2, Name = "B栋", ProjectId = 1, CreatedAt = now },
            new Building { Id = 3, Name = "C栋", ProjectId = 1, CreatedAt = now }
        );
        modelBuilder.Entity<AccessDoor>().HasData(
            new AccessDoor { Id = 1, Name = "A栋大门", BuildingId = 1, Location = "A栋1楼入口", Status = "online", CreatedAt = now },
            new AccessDoor { Id = 2, Name = "A栋电梯", BuildingId = 1, Location = "A栋电梯厅", Status = "online", CreatedAt = now },
            new AccessDoor { Id = 3, Name = "B栋大门", BuildingId = 2, Location = "B栋1楼入口", Status = "online", CreatedAt = now }
        );
        modelBuilder.Entity<AccessCard>().HasData(
            new AccessCard { Id = 1, CardNo = "CARD-A-001", CardUid = "UID001", OwnerName = "张三", OwnerPhone = "13800138001", Type = "owner", Status = "active", BuildingId = 1, Floor = "5", IssueDate = now.AddMonths(-3), ExpireDate = now.AddYears(1), ProjectId = 1, CreatedAt = now },
            new AccessCard { Id = 2, CardNo = "CARD-A-002", CardUid = "UID002", OwnerName = "李四", OwnerPhone = "13800138002", Type = "tenant", Status = "active", BuildingId = 1, Floor = "8", IssueDate = now.AddMonths(-1), ProjectId = 1, CreatedAt = now }
        );
    }
}