using Microsoft.EntityFrameworkCore;
using WO.Property.ExpressService.Models;

namespace WO.Property.ExpressService.Data;

public class ExpressDbContext : DbContext
{
    public ExpressDbContext(DbContextOptions<ExpressDbContext> options) : base(options) { }
    public DbSet<ExpressCompany> Companies { get; set; }
    public DbSet<ExpressDelivery> Deliveries { get; set; }
    public DbSet<ExpressNotification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<ExpressCompany>(e => { e.HasKey(x => x.Id); e.HasIndex(x => x.Name); });
        modelBuilder.Entity<ExpressDelivery>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.TrackingNumber).IsUnique();
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.RoomNumber);
            e.Property(x => x.Status).HasConversion<string>();
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<ExpressNotification>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ExpressId);
            e.Property(x => x.Type).HasConversion<string>();
            e.Property(x => x.Status).HasConversion<string>();
        });
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        var now = DateTime.UtcNow;
        modelBuilder.Entity<ExpressCompany>().HasData(
            new ExpressCompany { Id = 1, Name = "顺丰速运", Code = "SF", Phone = "95338", IsActive = true, CreatedAt = now },
            new ExpressCompany { Id = 2, Name = "圆通速递", Code = "YT", Phone = "95554", IsActive = true, CreatedAt = now },
            new ExpressCompany { Id = 3, Name = "中通快递", Code = "ZT", Phone = "95311", IsActive = true, CreatedAt = now },
            new ExpressCompany { Id = 4, Name = "韵达快递", Code = "YD", Phone = "95546", IsActive = true, CreatedAt = now },
            new ExpressCompany { Id = 5, Name = "申通快递", Code = "ST", Phone = "95543", IsActive = true, CreatedAt = now },
            new ExpressCompany { Id = 6, Name = "京东物流", Code = "JD", Phone = "950616", IsActive = true, CreatedAt = now },
            new ExpressCompany { Id = 7, Name = "邮政EMS", Code = "EMS", Phone = "11183", IsActive = true, CreatedAt = now },
            new ExpressCompany { Id = 8, Name = "菜鸟驿站", Code = "CN", IsActive = true, CreatedAt = now },
            new ExpressCompany { Id = 9, Name = "其他", Code = "OTHER", IsActive = true, CreatedAt = now }
        );
        modelBuilder.Entity<ExpressDelivery>().HasData(
            new ExpressDelivery { Id = 1, TrackingNumber = "SF1234567890", ExpressNumber = "EXP-2026-0001", CompanyId = 1, SenderName = "张先生", ReceiverName = "王女士", ReceiverPhone = "139-0000-2222", RoomNumber = "A栋1001", PickupAddress = "A栋大堂快递柜", Status = ExpressStatus.InStorage, ReceivedAt = now.AddDays(-1), ExpiresAt = now.AddDays(2), ReceivedBy = "物业前台", CreatedAt = now.AddDays(-1), UpdatedAt = now.AddDays(-1) },
            new ExpressDelivery { Id = 2, TrackingNumber = "YT9876543210", ExpressNumber = "EXP-2026-0002", CompanyId = 2, SenderName = "李先生", ReceiverName = "赵先生", ReceiverPhone = "136-0000-4444", RoomNumber = "B栋202", PickupAddress = "B栋大堂快递柜", Status = ExpressStatus.Pending, ReceivedAt = now.AddHours(-3), ExpiresAt = now.AddDays(3), CreatedAt = now.AddHours(-3), UpdatedAt = now.AddHours(-3) },
            new ExpressDelivery { Id = 3, TrackingNumber = "JD5678901234", ExpressNumber = "EXP-2026-0003", CompanyId = 6, SenderName = "京东商城", ReceiverName = "孙小姐", ReceiverPhone = "135-0000-5555", RoomNumber = "C栋301", PickupAddress = "C栋前台", Status = ExpressStatus.Delivered, ReceivedAt = now.AddDays(-2), PickedUpAt = now.AddDays(-1), ExpiresAt = now.AddDays(1), ReceivedBy = "孙小姐", CreatedAt = now.AddDays(-2), UpdatedAt = now.AddDays(-1) }
        );
    }
}
