using Microsoft.EntityFrameworkCore;
using WO.Property.DeliveryService.Models;

namespace WO.Property.DeliveryService.Data;

public class DeliveryDbContext : DbContext
{
    public DeliveryDbContext(DbContextOptions<DeliveryDbContext> options) : base(options) { }
    public DbSet<Rider> Riders { get; set; }
    public DbSet<FoodDelivery> Deliveries { get; set; }
    public DbSet<DeliveryNotification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Rider>(e => { e.HasKey(x => x.Id); e.HasIndex(x => x.Phone); });
        modelBuilder.Entity<FoodDelivery>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.OrderNumber).IsUnique();
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.RoomNumber);
            e.Property(x => x.Status).HasConversion<string>();
            e.Property(x => x.PaymentMethod).HasConversion<string>();
            e.Property(x => x.PaymentStatus).HasConversion<string>();
            e.HasOne(x => x.Rider).WithMany().HasForeignKey(x => x.RiderId).OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<DeliveryNotification>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.DeliveryId);
            e.Property(x => x.Status).HasConversion<string>();
        });
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        var now = DateTime.UtcNow;
        modelBuilder.Entity<Rider>().HasData(
            new Rider { Id = 1, Name = "骑手A", Phone = "150-0001-0001", Platform = "美团", PlateNumber = "京A12345", IsActive = true, CreatedAt = now },
            new Rider { Id = 2, Name = "骑手B", Phone = "150-0002-0002", Platform = "饿了么", PlateNumber = "京B67890", IsActive = true, CreatedAt = now },
            new Rider { Id = 3, Name = "骑手C", Phone = "150-0003-0003", Platform = "美团", IsActive = true, CreatedAt = now }
        );
        modelBuilder.Entity<FoodDelivery>().HasData(
            new FoodDelivery { Id = 1, OrderNumber = "ORDER-2026-0001", MerchantName = "麦当劳", CustomerName = "王女士", CustomerPhone = "139-0000-2222", RoomNumber = "A栋1001", DeliveryAddress = "A栋大堂", TotalAmount = 68.50m, PaymentMethod = PaymentMethod.Online, PaymentStatus = PaymentStatus.Paid, Status = DeliveryStatus.Ready, RiderId = 1, ReceivedAt = now.AddMinutes(-15), CreatedAt = now.AddMinutes(-20), UpdatedAt = now.AddMinutes(-15) },
            new FoodDelivery { Id = 2, OrderNumber = "ORDER-2026-0002", MerchantName = "肯德基宅急送", CustomerName = "李先生", CustomerPhone = "138-0000-3333", RoomNumber = "B栋202", TotalAmount = 95.00m, PaymentMethod = PaymentMethod.Online, PaymentStatus = PaymentStatus.Paid, Status = DeliveryStatus.InDelivery, RiderId = 2, ReceivedAt = now.AddMinutes(-5), CreatedAt = now.AddMinutes(-10), UpdatedAt = now.AddMinutes(-5) },
            new FoodDelivery { Id = 3, OrderNumber = "ORDER-2026-0003", MerchantName = "海底捞外送", CustomerName = "赵先生", CustomerPhone = "137-0000-4444", RoomNumber = "C栋301", TotalAmount = 328.00m, PaymentMethod = PaymentMethod.Cash, PaymentStatus = PaymentStatus.Paid, Status = DeliveryStatus.Delivered, RiderId = 1, ReceivedAt = now.AddHours(-1), DeliveredAt = now.AddMinutes(-45), CreatedAt = now.AddHours(-2), UpdatedAt = now.AddMinutes(-45) }
        );
    }
}
