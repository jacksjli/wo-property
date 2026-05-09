using Microsoft.EntityFrameworkCore;
using WO.Property.ContractService.Models;

namespace WO.Property.ContractService.Data;

public class ContractDbContext : DbContext
{
    public ContractDbContext(DbContextOptions<ContractDbContext> options) : base(options)
    {
    }
    
    public DbSet<Contract> Contracts { get; set; }
    public DbSet<Payment> Payments { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Contract 配置
        modelBuilder.Entity<Contract>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ContractNumber).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.StartDate);
            entity.HasIndex(e => e.EndDate);
            
            entity.Property(e => e.Type)
                .HasConversion<string>();
            
            entity.Property(e => e.Status)
                .HasConversion<string>();
        });
        
        // Payment 配置
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PaymentNumber).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.DueDate);
            
            entity.HasOne(e => e.Contract)
                .WithMany()
                .HasForeignKey(e => e.ContractId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.Property(e => e.Status)
                .HasConversion<string>();
        });
        
        // 种子数据
        SeedData(modelBuilder);
    }
    
    private void SeedData(ModelBuilder modelBuilder)
    {
        var now = DateTime.UtcNow;
        
        // 种子合同
        modelBuilder.Entity<Contract>().HasData(
            new Contract
            {
                Id = 1,
                ContractNumber = "HT-2026-0001",
                Type = ContractType.Lease,
                Title = "A栋1001室租赁合同",
                Description = "办公室租赁合同，建筑面积120平方米",
                PartyA = "WO物业管理有限公司",
                PartyAContact = "张经理",
                PartyAPhone = "021-12345678",
                PartyB = "上海科技有限公司",
                PartyBContact = "李总",
                PartyBPhone = "138-0000-1001",
                Amount = 36000m,
                Currency = "CNY",
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 12, 31),
                Status = ContractStatus.Active,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = "system"
            },
            new Contract
            {
                Id = 2,
                ContractNumber = "HT-2026-0002",
                Type = ContractType.Property,
                Title = "B栋物业服务合同",
                Description = "年度物业服务合同，服务范围包括公共区域清洁、绿化维护等",
                PartyA = "WO物业管理有限公司",
                PartyAContact = "王经理",
                PartyAPhone = "021-12345678",
                PartyB = "B栋全体业主委员会",
                PartyBContact = "陈主任",
                PartyBPhone = "139-0000-1002",
                Amount = 120000m,
                Currency = "CNY",
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 12, 31),
                Status = ContractStatus.Active,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = "system"
            },
            new Contract
            {
                Id = 3,
                ContractNumber = "HT-2026-0003",
                Type = ContractType.Maintenance,
                Title = "电梯维保合同",
                Description = "3号楼2部电梯的季度维护保养合同",
                PartyA = "电梯维保公司",
                PartyAContact = "刘师傅",
                PartyAPhone = "021-88888888",
                PartyB = "WO物业管理有限公司",
                PartyBContact = "张经理",
                PartyBPhone = "021-12345678",
                Amount = 24000m,
                Currency = "CNY",
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 6, 30),
                Status = ContractStatus.ExpiringSoon,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = "system"
            },
            new Contract
            {
                Id = 4,
                ContractNumber = "HT-2025-0099",
                Type = ContractType.Service,
                Title = "清洁服务合同(2025)",
                Description = "2025年度公共区域清洁服务合同",
                PartyA = "清洁服务公司",
                PartyAContact = "赵经理",
                PartyAPhone = "021-77777777",
                PartyB = "WO物业管理有限公司",
                PartyBContact = "张经理",
                PartyBPhone = "021-12345678",
                Amount = 60000m,
                Currency = "CNY",
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2025, 12, 31),
                Status = ContractStatus.Expired,
                CreatedAt = now.AddYears(-1),
                UpdatedAt = now.AddYears(-1),
                CreatedBy = "system"
            }
        );
        
        // 种子付款记录
        modelBuilder.Entity<Payment>().HasData(
            new Payment
            {
                Id = 1,
                ContractId = 1,
                PaymentNumber = "PAY-2026-0001",
                Description = "2026年1季度租金",
                Amount = 9000m,
                Currency = "CNY",
                DueDate = new DateTime(2026, 1, 15),
                PaidDate = new DateTime(2026, 1, 10),
                Status = PaymentStatus.Paid,
                CreatedAt = now,
                UpdatedAt = now
            },
            new Payment
            {
                Id = 2,
                ContractId = 1,
                PaymentNumber = "PAY-2026-0002",
                Description = "2026年2季度租金",
                Amount = 9000m,
                Currency = "CNY",
                DueDate = new DateTime(2026, 4, 15),
                PaidDate = new DateTime(2026, 4, 5),
                Status = PaymentStatus.Paid,
                CreatedAt = now,
                UpdatedAt = now
            },
            new Payment
            {
                Id = 3,
                ContractId = 1,
                PaymentNumber = "PAY-2026-0003",
                Description = "2026年3季度租金",
                Amount = 9000m,
                Currency = "CNY",
                DueDate = new DateTime(2026, 7, 15),
                Status = PaymentStatus.Pending,
                CreatedAt = now,
                UpdatedAt = now
            },
            new Payment
            {
                Id = 4,
                ContractId = 2,
                PaymentNumber = "PAY-2026-0004",
                Description = "2026年物业费(年付)",
                Amount = 120000m,
                Currency = "CNY",
                DueDate = new DateTime(2026, 1, 31),
                PaidDate = new DateTime(2026, 1, 20),
                Status = PaymentStatus.Paid,
                CreatedAt = now,
                UpdatedAt = now
            },
            new Payment
            {
                Id = 5,
                ContractId = 3,
                PaymentNumber = "PAY-2026-0005",
                Description = "电梯维保费(上半年)",
                Amount = 12000m,
                Currency = "CNY",
                DueDate = new DateTime(2026, 3, 31),
                PaidDate = new DateTime(2026, 3, 25),
                Status = PaymentStatus.Paid,
                CreatedAt = now,
                UpdatedAt = now
            }
        );
    }
}
