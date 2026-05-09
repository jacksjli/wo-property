using Microsoft.EntityFrameworkCore;
using WO.Property.VisitorService.Models;

namespace WO.Property.VisitorService.Data;

public class VisitorDbContext : DbContext
{
    public VisitorDbContext(DbContextOptions<VisitorDbContext> options) : base(options)
    {
    }
    
    public DbSet<Visitor> Visitors { get; set; }
    public DbSet<VisitRecord> VisitRecords { get; set; }
    public DbSet<VisitStatistics> VisitStatistics { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Visitor 配置
        modelBuilder.Entity<Visitor>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.VisitorNumber).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ScheduledDate);
            entity.Property(e => e.Type).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.IDType).HasConversion<string>();
        });
        
        // VisitRecord 配置
        modelBuilder.Entity<VisitRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.RecordNumber).IsUnique();
            entity.HasIndex(e => e.AccessTime);
            
            entity.HasOne(e => e.Visitor)
                .WithMany()
                .HasForeignKey(e => e.VisitorId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // VisitStatistics 配置
        modelBuilder.Entity<VisitStatistics>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.StatDate).IsUnique();
        });
        
        // 种子数据
        SeedData(modelBuilder);
    }
    
    private void SeedData(ModelBuilder modelBuilder)
    {
        var now = DateTime.UtcNow;
        var today = DateTime.Today;
        
        // 访客数据
        modelBuilder.Entity<Visitor>().HasData(
            new Visitor
            {
                Id = 1,
                VisitorNumber = "VIS-2026-0001",
                VisitorName = "张先生",
                VisitorPhone = "138-0001-0001",
                IDType = IDType.IDCard,
                IDNumber = "110101199001011234",
                Type = VisitType.Business,
                HostName = "王经理",
                HostPhone = "138-1000-1001",
                HostUnit = "行政部",
                VisitLocation = "A栋2楼会议室",
                ScheduledDate = today,
                ScheduledStartTime = today.AddHours(10),
                ScheduledEndTime = today.AddHours(12),
                Purpose = "商务合作洽谈",
                ExpectedVisitors = 2,
                Status = VisitStatus.Approved,
                Approver = "张物业",
                ApprovedDate = now.AddHours(-2),
                AccessCode = "V20260001",
                AccessGranted = true,
                CreatedAt = now.AddHours(-3),
                UpdatedAt = now.AddHours(-2)
            },
            new Visitor
            {
                Id = 2,
                VisitorNumber = "VIS-2026-0002",
                VisitorName = "李师傅",
                VisitorPhone = "139-0002-0002",
                IDType = IDType.IDCard,
                IDNumber = "110101199002022345",
                Type = VisitType.Maintenance,
                HostName = "赵工程师",
                HostPhone = "139-1000-2002",
                HostUnit = "工程部",
                VisitLocation = "B栋地下室",
                ScheduledDate = today,
                ScheduledStartTime = today.AddHours(14),
                ScheduledEndTime = today.AddHours(17),
                Purpose = "电梯维修",
                Status = VisitStatus.Pending,
                CreatedAt = now.AddHours(-1),
                UpdatedAt = now.AddHours(-1)
            },
            new Visitor
            {
                Id = 3,
                VisitorNumber = "VIS-2026-0003",
                VisitorName = "王快递",
                VisitorPhone = "137-0003-0003",
                Type = VisitType.Delivery,
                HostName = "前台",
                VisitLocation = "前台",
                ScheduledDate = today,
                ScheduledStartTime = today.AddHours(9),
                ScheduledEndTime = today.AddHours(21),
                Purpose = "快递签收",
                Status = VisitStatus.CheckIn,
                Approver = "张物业",
                ApprovedDate = now.AddDays(-1),
                AccessCode = "V20260003",
                AccessGranted = true,
                ActualCheckInTime = now.AddHours(-1),
                CreatedAt = now.AddDays(-1),
                UpdatedAt = now.AddHours(-1)
            },
            new Visitor
            {
                Id = 4,
                VisitorNumber = "VIS-2026-0004",
                VisitorName = "陈小姐",
                VisitorPhone = "136-0004-0004",
                IDType = IDType.IDCard,
                IDNumber = "110101199003033456",
                Type = VisitType.Interview,
                HostName = "刘HR",
                HostPhone = "138-1000-4004",
                HostUnit = "人力资源部",
                VisitLocation = "A栋3楼会议室",
                ScheduledDate = today.AddDays(1),
                ScheduledStartTime = today.AddDays(1).AddHours(10),
                ScheduledEndTime = today.AddDays(1).AddHours(12),
                Purpose = "面试",
                ExpectedVisitors = 1,
                Status = VisitStatus.Approved,
                Approver = "张物业",
                ApprovedDate = now,
                AccessCode = "V20260004",
                AccessGranted = true,
                LicensePlate = "京A12345",
                CreatedAt = now,
                UpdatedAt = now
            },
            new Visitor
            {
                Id = 5,
                VisitorNumber = "VIS-2026-0005",
                VisitorName = "周先生",
                VisitorPhone = "135-0005-0005",
                IDType = IDType.IDCard,
                IDNumber = "110101199004044567",
                Type = VisitType.Personal,
                HostName = "业主张先生",
                HostPhone = "138-1000-5005",
                VisitLocation = "业主家",
                ScheduledDate = today.AddDays(-1),
                ScheduledStartTime = today.AddDays(-1).AddHours(15),
                ScheduledEndTime = today.AddDays(-1).AddHours(18),
                Purpose = "探访",
                Status = VisitStatus.CheckOut,
                Approver = "张物业",
                ApprovedDate = now.AddDays(-1).AddHours(-2),
                ActualCheckInTime = now.AddDays(-1).AddHours(15),
                ActualCheckOutTime = now.AddDays(-1).AddHours(18),
                CreatedAt = now.AddDays(-1).AddHours(-3),
                UpdatedAt = now.AddDays(-1)
            }
        );
        
        // 访问记录
        modelBuilder.Entity<VisitRecord>().HasData(
            new VisitRecord
            {
                Id = 1,
                VisitorId = 3,
                RecordNumber = "REC-2026-0001",
                GateDevice = "大门1号机",
                AccessDirection = "In",
                AccessTime = now.AddHours(-1),
                RegisteredBy = "张物业",
                Remarks = "快递员签到",
                CreatedAt = now.AddHours(-1)
            },
            new VisitRecord
            {
                Id = 2,
                VisitorId = 5,
                RecordNumber = "REC-2026-0002",
                GateDevice = "大门1号机",
                AccessDirection = "In",
                AccessTime = now.AddDays(-1).AddHours(15),
                Temperature = 36.5m,
                HealthCodeStatus = "绿码",
                RegisteredBy = "张物业",
                CreatedAt = now.AddDays(-1).AddHours(15)
            },
            new VisitRecord
            {
                Id = 3,
                VisitorId = 5,
                RecordNumber = "REC-2026-0003",
                GateDevice = "大门1号机",
                AccessDirection = "Out",
                AccessTime = now.AddDays(-1).AddHours(18),
                RegisteredBy = "张物业",
                CreatedAt = now.AddDays(-1).AddHours(18)
            }
        );
        
        // 统计数据
        modelBuilder.Entity<VisitStatistics>().HasData(
            new VisitStatistics
            {
                Id = 1,
                StatDate = today,
                TotalVisits = 3,
                TotalVisitors = 3,
                CheckedIn = 1,
                CheckedOut = 1,
                Pending = 1,
                Cancelled = 0,
                Expired = 0,
                PersonalVisits = 1,
                BusinessVisits = 1,
                DeliveryVisits = 1,
                MaintenanceVisits = 0,
                CreatedAt = now
            },
            new VisitStatistics
            {
                Id = 2,
                StatDate = today.AddDays(-1),
                TotalVisits = 5,
                TotalVisitors = 4,
                CheckedIn = 5,
                CheckedOut = 5,
                Pending = 0,
                Cancelled = 0,
                Expired = 0,
                PersonalVisits = 2,
                BusinessVisits = 1,
                DeliveryVisits = 2,
                MaintenanceVisits = 0,
                CreatedAt = now.AddDays(-1)
            }
        );
    }
}
