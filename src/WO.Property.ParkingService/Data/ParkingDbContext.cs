using Microsoft.EntityFrameworkCore;
using WO.Property.ParkingService.Models;

namespace WO.Property.ParkingService.Data;

public class ParkingDbContext : DbContext
{
    public ParkingDbContext(DbContextOptions<ParkingDbContext> options) : base(options) { }

    public DbSet<ParkingLot> ParkingLots { get; set; }
    public DbSet<ParkingSpace> ParkingSpaces { get; set; }
    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<ParkingRecord> ParkingRecords { get; set; }
    public DbSet<ParkingPayment> ParkingPayments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ParkingLot>(e => {
            e.HasIndex(l => l.ProjectId);
        });

        modelBuilder.Entity<ParkingSpace>(e => {
            e.HasIndex(s => s.LotId);
            e.HasIndex(s => s.Status);
        });

        modelBuilder.Entity<Vehicle>(e => {
            e.HasIndex(v => v.PlateNumber).IsUnique();
            e.HasIndex(v => v.ProjectId);
        });

        modelBuilder.Entity<ParkingRecord>(e => {
            e.HasIndex(r => r.PlateNumber);
            e.HasIndex(r => r.LotId);
            e.HasIndex(r => r.Status);
            e.HasIndex(r => r.EntryTime);
        });

        modelBuilder.Entity<ParkingPayment>(e => {
            e.HasIndex(p => p.PaymentNo).IsUnique();
        });

        // 种子数据
        var now = DateTime.UtcNow;
        modelBuilder.Entity<ParkingLot>().HasData(
            new ParkingLot { Id = 1, Name = "A区停车场", Location = "A栋楼下", TotalSpaces = 50, HourlyRate = 5m, MonthlyRate = 300m, ProjectId = 1, CreatedAt = now },
            new ParkingLot { Id = 2, Name = "B区停车场", Location = "B栋地下", TotalSpaces = 80, HourlyRate = 4m, MonthlyRate = 280m, ProjectId = 1, CreatedAt = now }
        );

        modelBuilder.Entity<Vehicle>().HasData(
            new Vehicle { Id = 1, PlateNumber = "粤B12345", Brand = "丰田", Color = "黑色", OwnerName = "张三", OwnerPhone = "13800138001", Type = "monthly", ProjectId = 1, CreatedAt = now },
            new Vehicle { Id = 2, PlateNumber = "粤B67890", Brand = "本田", Color = "白色", OwnerName = "李四", OwnerPhone = "13800138002", Type = "monthly", ProjectId = 1, CreatedAt = now }
        );
    }
}