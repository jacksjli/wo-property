using Microsoft.EntityFrameworkCore;
using WO.Property.KeyService.Models;

namespace WO.Property.KeyService.Data;

public class KeyDbContext : DbContext
{
    public KeyDbContext(DbContextOptions<KeyDbContext> options) : base(options)
    {
    }
    
    public DbSet<Key> Keys { get; set; }
    public DbSet<KeyBorrow> Borrows { get; set; }
    public DbSet<KeyUsageLog> UsageLogs { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Key 配置
        modelBuilder.Entity<Key>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.KeyNumber).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.Property(e => e.Status).HasConversion<string>();
        });
        
        // KeyBorrow 配置
        modelBuilder.Entity<KeyBorrow>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.BorrowNumber).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.BorrowDate);
            entity.Property(e => e.Status).HasConversion<string>();
            
            entity.HasOne(e => e.Key)
                .WithMany()
                .HasForeignKey(e => e.KeyId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // KeyUsageLog 配置
        modelBuilder.Entity<KeyUsageLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ActionTime);
            
            entity.HasOne(e => e.Key)
                .WithMany()
                .HasForeignKey(e => e.KeyId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // 种子数据
        SeedData(modelBuilder);
    }
    
    private void SeedData(ModelBuilder modelBuilder)
    {
        var now = DateTime.UtcNow;
        
        // 钥匙数据
        modelBuilder.Entity<Key>().HasData(
            new Key
            {
                Id = 1,
                KeyNumber = "KEY-A101",
                Name = "A栋101办公室钥匙",
                Description = "A栋1楼101办公室主钥匙",
                Location = "A栋101办公室",
                RoomNumber = "A栋101",
                Status = KeyStatus.Available,
                TotalCopies = 2,
                AvailableCopies = 2,
                StorageLocation = "前台钥匙柜-A1",
                CreatedAt = now,
                UpdatedAt = now
            },
            new Key
            {
                Id = 2,
                KeyNumber = "KEY-A102",
                Name = "A栋102会议室钥匙",
                Description = "A栋1楼102会议室钥匙",
                Location = "A栋102会议室",
                RoomNumber = "A栋102",
                Status = KeyStatus.Borrowed,
                TotalCopies = 2,
                AvailableCopies = 1,
                StorageLocation = "前台钥匙柜-A2",
                CreatedAt = now,
                UpdatedAt = now
            },
            new Key
            {
                Id = 3,
                KeyNumber = "KEY-MGR",
                Name = "物业经理办公室钥匙",
                Description = "物业经理办公室主钥匙",
                Location = "物业管理处",
                RoomNumber = "物业处",
                Status = KeyStatus.Available,
                TotalCopies = 1,
                AvailableCopies = 1,
                StorageLocation = "前台钥匙柜-MGR",
                CreatedAt = now,
                UpdatedAt = now
            },
            new Key
            {
                Id = 4,
                KeyNumber = "KEY-PARKING",
                Name = "停车场管理室钥匙",
                Description = "停车场收费室钥匙",
                Location = "停车场收费室",
                RoomNumber = "停车场",
                Status = KeyStatus.Available,
                TotalCopies = 3,
                AvailableCopies = 3,
                StorageLocation = "安保室钥匙柜-P",
                CreatedAt = now,
                UpdatedAt = now
            },
            new Key
            {
                Id = 5,
                KeyNumber = "KEY-ELEVATOR-M",
                Name = "电梯机房门钥匙",
                Description = "电梯机房主钥匙，所有电梯通用",
                Location = "各楼栋电梯机房",
                Status = KeyStatus.Available,
                TotalCopies = 2,
                AvailableCopies = 2,
                StorageLocation = "工程部钥匙柜-E",
                Remarks = "仅限工程人员使用",
                CreatedAt = now,
                UpdatedAt = now
            },
            new Key
            {
                Id = 6,
                KeyNumber = "KEY-FIRE",
                Name = "消防设备室钥匙",
                Description = "消防设备间主钥匙",
                Location = "各楼栋消防设备间",
                Status = KeyStatus.Lost,
                TotalCopies = 2,
                AvailableCopies = 0,
                StorageLocation = "遗失",
                Remarks = "正在配钥匙中",
                CreatedAt = now.AddMonths(-1),
                UpdatedAt = now.AddDays(-5)
            }
        );
        
        // 借用记录
        modelBuilder.Entity<KeyBorrow>().HasData(
            new KeyBorrow
            {
                Id = 1,
                BorrowNumber = "BRW-2026-0001",
                KeyId = 2,
                BorrowerName = "王经理",
                BorrowerPhone = "138-0000-2001",
                BorrowerUnit = "行政部",
                BorrowDate = now.AddDays(-1),
                ExpectedReturnDate = now.AddDays(1),
                Status = BorrowStatus.Borrowed,
                Purpose = "会议室布置",
                Approver = "张物业",
                ApprovedDate = now.AddDays(-1),
                ApprovedRemarks = "同意借用",
                CreatedAt = now.AddDays(-1),
                UpdatedAt = now.AddDays(-1)
            },
            new KeyBorrow
            {
                Id = 2,
                BorrowNumber = "BRW-2026-0002",
                KeyId = 1,
                BorrowerName = "李同事",
                BorrowerPhone = "139-0000-1002",
                BorrowerUnit = "财务部",
                BorrowDate = now.AddDays(-3),
                ExpectedReturnDate = now.AddDays(-1),
                ActualReturnDate = now.AddDays(-2),
                Status = BorrowStatus.Returned,
                Purpose = "取文件",
                Approver = "张物业",
                ApprovedDate = now.AddDays(-3),
                ReturnReceiver = "张物业",
                ReturnRemarks = "钥匙完好",
                KeyReturned = true,
                KeyConditionOk = true,
                CreatedAt = now.AddDays(-3),
                UpdatedAt = now.AddDays(-2)
            },
            new KeyBorrow
            {
                Id = 3,
                BorrowNumber = "BRW-2026-0003",
                KeyId = 4,
                BorrowerName = "赵师傅",
                BorrowerPhone = "137-0000-4004",
                BorrowerUnit = "工程部",
                BorrowDate = now,
                ExpectedReturnDate = now.AddHours(4),
                Status = BorrowStatus.Approved,
                Purpose = "停车场设备检修",
                Approver = "张物业",
                ApprovedDate = now.AddHours(-1),
                ApprovedRemarks = "批准使用4小时",
                CreatedAt = now.AddHours(-1),
                UpdatedAt = now
            }
        );
        
        // 使用记录
        modelBuilder.Entity<KeyUsageLog>().HasData(
            new KeyUsageLog
            {
                Id = 1,
                KeyId = 2,
                BorrowId = 1,
                Action = "Borrow",
                Operator = "王经理",
                Remarks = "借用会议室钥匙",
                ActionTime = now.AddDays(-1)
            },
            new KeyUsageLog
            {
                Id = 2,
                KeyId = 1,
                BorrowId = 2,
                Action = "Borrow",
                Operator = "李同事",
                Remarks = "借用办公室钥匙",
                ActionTime = now.AddDays(-3)
            },
            new KeyUsageLog
            {
                Id = 3,
                KeyId = 1,
                BorrowId = 2,
                Action = "Return",
                Operator = "张物业",
                Remarks = "钥匙已归还，状态良好",
                ActionTime = now.AddDays(-2)
            },
            new KeyUsageLog
            {
                Id = 4,
                KeyId = 6,
                Action = "Lost",
                Operator = "系统",
                Remarks = "消防设备室钥匙遗失",
                ActionTime = now.AddDays(-5)
            }
        );
    }
}
