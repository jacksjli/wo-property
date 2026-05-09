using Microsoft.EntityFrameworkCore;
using WO.Property.FinanceService.Models;

namespace WO.Property.FinanceService.Data;

public class FinanceDbContext : DbContext
{
    public FinanceDbContext(DbContextOptions<FinanceDbContext> options) : base(options)
    {
    }
    
    public DbSet<FeeItem> FeeItems { get; set; }
    public DbSet<Bill> Bills { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Receipt> Receipts { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // FeeItem 配置
        modelBuilder.Entity<FeeItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Type);
            entity.Property(e => e.Type).HasConversion<string>();
        });
        
        // Bill 配置
        modelBuilder.Entity<Bill>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.BillNumber).IsUnique();
            entity.HasIndex(e => e.RoomNumber);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.DueDate);
            entity.Property(e => e.FeeType).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();
        });
        
        // Payment 配置
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PaymentNumber).IsUnique();
            entity.HasIndex(e => e.BillId);
            entity.HasIndex(e => e.PaymentDate);
            entity.Property(e => e.PaymentMethod).HasConversion<string>();
            
            entity.HasOne(e => e.Bill)
                .WithMany()
                .HasForeignKey(e => e.BillId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Receipt 配置
        modelBuilder.Entity<Receipt>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ReceiptNumber).IsUnique();
            entity.HasIndex(e => e.IssuedDate);
            
            entity.HasOne(e => e.Payment)
                .WithMany()
                .HasForeignKey(e => e.PaymentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // 种子数据
        SeedData(modelBuilder);
    }
    
    private void SeedData(ModelBuilder modelBuilder)
    {
        var now = DateTime.UtcNow;
        
        // 费用科目
        modelBuilder.Entity<FeeItem>().HasData(
            new FeeItem
            {
                Id = 1,
                Name = "物业管理费",
                Type = FeeType.PropertyFee,
                UnitPrice = 3.5m,
                Unit = "元/平方米/月",
                Description = "基础物业管理服务费",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new FeeItem
            {
                Id = 2,
                Name = "停车费",
                Type = FeeType.ParkingFee,
                UnitPrice = 300m,
                Unit = "元/车位/月",
                Description = "地下停车场车位租赁费",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new FeeItem
            {
                Id = 3,
                Name = "水费",
                Type = FeeType.WaterFee,
                UnitPrice = 4.2m,
                Unit = "元/吨",
                Description = "居民用水价格",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new FeeItem
            {
                Id = 4,
                Name = "电费",
                Type = FeeType.ElectricityFee,
                UnitPrice = 0.6m,
                Unit = "元/度",
                Description = "公共用电分摊",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new FeeItem
            {
                Id = 5,
                Name = "供暖费",
                Type = FeeType.HeatingFee,
                UnitPrice = 25m,
                Unit = "元/平方米/季",
                Description = "冬季供暖费",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            }
        );
        
        // 账单数据
        modelBuilder.Entity<Bill>().HasData(
            new Bill
            {
                Id = 1,
                BillNumber = "BILL-2026-0001",
                RoomNumber = "A栋1001",
                OwnerName = "李明",
                OwnerPhone = "138-0000-1001",
                FeeType = FeeType.PropertyFee,
                Amount = 1260m,
                DiscountAmount = 0,
                ActualAmount = 1260m,
                Currency = "CNY",
                BillingPeriodStart = new DateTime(2026, 1, 1),
                BillingPeriodEnd = new DateTime(2026, 3, 31),
                DueDate = new DateTime(2026, 1, 31),
                PaidDate = new DateTime(2026, 1, 15),
                Status = BillStatus.Paid,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = "system"
            },
            new Bill
            {
                Id = 2,
                BillNumber = "BILL-2026-0002",
                RoomNumber = "A栋1001",
                OwnerName = "李明",
                OwnerPhone = "138-0000-1001",
                FeeType = FeeType.ParkingFee,
                Amount = 900m,
                DiscountAmount = 0,
                ActualAmount = 900m,
                Currency = "CNY",
                BillingPeriodStart = new DateTime(2026, 1, 1),
                BillingPeriodEnd = new DateTime(2026, 3, 31),
                DueDate = new DateTime(2026, 1, 31),
                PaidDate = new DateTime(2026, 1, 15),
                Status = BillStatus.Paid,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = "system"
            },
            new Bill
            {
                Id = 3,
                BillNumber = "BILL-2026-0003",
                RoomNumber = "B栋202",
                OwnerName = "王芳",
                OwnerPhone = "139-0000-2022",
                FeeType = FeeType.PropertyFee,
                Amount = 980m,
                DiscountAmount = 50m,
                ActualAmount = 930m,
                Currency = "CNY",
                BillingPeriodStart = new DateTime(2026, 4, 1),
                BillingPeriodEnd = new DateTime(2026, 6, 30),
                DueDate = new DateTime(2026, 4, 30),
                Status = BillStatus.Pending,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = "system"
            },
            new Bill
            {
                Id = 4,
                BillNumber = "BILL-2026-0004",
                RoomNumber = "C栋301",
                OwnerName = "张伟",
                OwnerPhone = "137-0000-3033",
                FeeType = FeeType.WaterFee,
                Amount = 168m,
                DiscountAmount = 0,
                ActualAmount = 168m,
                Currency = "CNY",
                BillingPeriodStart = new DateTime(2026, 3, 1),
                BillingPeriodEnd = new DateTime(2026, 3, 31),
                DueDate = new DateTime(2026, 3, 31),
                Status = BillStatus.Overdue,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = "system"
            },
            new Bill
            {
                Id = 5,
                BillNumber = "BILL-2026-0005",
                RoomNumber = "A栋502",
                OwnerName = "刘洋",
                OwnerPhone = "136-0000-5052",
                FeeType = FeeType.HeatingFee,
                Amount = 1500m,
                DiscountAmount = 0,
                ActualAmount = 1500m,
                Currency = "CNY",
                BillingPeriodStart = new DateTime(2025, 11, 15),
                BillingPeriodEnd = new DateTime(2026, 3, 15),
                DueDate = new DateTime(2025, 12, 31),
                Status = BillStatus.Paid,
                PaidDate = new DateTime(2025, 12, 20),
                CreatedAt = now.AddMonths(-3),
                UpdatedAt = now.AddMonths(-3),
                CreatedBy = "system"
            }
        );
        
        // 付款记录
        modelBuilder.Entity<Payment>().HasData(
            new Payment
            {
                Id = 1,
                PaymentNumber = "PAY-2026-0001",
                BillId = 1,
                Amount = 1260m,
                Currency = "CNY",
                PaymentMethod = PaymentMethod.WeChatPay,
                PaymentDate = new DateTime(2026, 1, 15),
                TransactionId = "WX2026011500001",
                CreatedAt = now,
                CreatedBy = "system"
            },
            new Payment
            {
                Id = 2,
                PaymentNumber = "PAY-2026-0002",
                BillId = 2,
                Amount = 900m,
                Currency = "CNY",
                PaymentMethod = PaymentMethod.Alipay,
                PaymentDate = new DateTime(2026, 1, 15),
                TransactionId = "ZFB2026011500002",
                CreatedAt = now,
                CreatedBy = "system"
            },
            new Payment
            {
                Id = 3,
                PaymentNumber = "PAY-2026-0003",
                BillId = 5,
                Amount = 1500m,
                Currency = "CNY",
                PaymentMethod = PaymentMethod.BankTransfer,
                PaymentDate = new DateTime(2025, 12, 20),
                TransactionId = "BANK2025122000003",
                CreatedAt = now.AddMonths(-3),
                CreatedBy = "system"
            }
        );
        
        // 收据
        modelBuilder.Entity<Receipt>().HasData(
            new Receipt
            {
                Id = 1,
                ReceiptNumber = "RCP-2026-0001",
                PaymentId = 1,
                PayerName = "李明",
                PayerPhone = "138-0000-1001",
                RoomNumber = "A栋1001",
                Amount = 1260m,
                Description = "2026年Q1物业费",
                IssuedDate = new DateTime(2026, 1, 15),
                CreatedAt = now
            },
            new Receipt
            {
                Id = 2,
                ReceiptNumber = "RCP-2026-0002",
                PaymentId = 2,
                PayerName = "李明",
                PayerPhone = "138-0000-1001",
                RoomNumber = "A栋1001",
                Amount = 900m,
                Description = "2026年Q1停车费",
                IssuedDate = new DateTime(2026, 1, 15),
                CreatedAt = now
            },
            new Receipt
            {
                Id = 3,
                ReceiptNumber = "RCP-2025-0003",
                PaymentId = 3,
                PayerName = "刘洋",
                PayerPhone = "136-0000-5052",
                RoomNumber = "A栋502",
                Amount = 1500m,
                Description = "2025-2026供暖季供暖费",
                IssuedDate = new DateTime(2025, 12, 20),
                CreatedAt = now.AddMonths(-3)
            }
        );
    }
}
