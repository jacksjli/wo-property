using Microsoft.EntityFrameworkCore;
using WO.Property.PaymentService.Models;

namespace WO.Property.PaymentService.Data;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

    public DbSet<Bill> Bills { get; set; }
    public DbSet<MeterReading> MeterReadings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Bill>(e => {
            e.HasIndex(b => b.BillNo).IsUnique();
            e.HasIndex(b => b.Status);
            e.HasIndex(b => b.BillingMonth);
        });
        modelBuilder.Entity<MeterReading>(e => { e.HasIndex(m => m.ReadingDate); });

        var now = DateTime.UtcNow;
        var thisMonth = new DateTime(now.Year, now.Month, 1);
        var lastMonth = thisMonth.AddMonths(-1);

        modelBuilder.Entity<Bill>().HasData(
            new Bill { Id = 1, BillNo = "BILL-2026-04-001", Type = "property", Description = "物业费", Unit = "A栋501", Amount = 800, BillingMonth = thisMonth, Status = "paid", PaidAt = now.AddDays(-10), TransactionId = "TXN-A001", ProjectId = 1, CreatedAt = thisMonth },
            new Bill { Id = 2, BillNo = "BILL-2026-04-002", Type = "parking", Description = "停车费-月租", Unit = "粤B12345", Amount = 300, BillingMonth = thisMonth, Status = "paid", PaidAt = now.AddDays(-5), TransactionId = "TXN-A002", ProjectId = 1, CreatedAt = thisMonth },
            new Bill { Id = 3, BillNo = "BILL-2026-04-003", Type = "water", Description = "水费", Unit = "A栋501", Amount = 180, BillingMonth = thisMonth, Status = "unpaid", ProjectId = 1, CreatedAt = thisMonth },
            new Bill { Id = 4, BillNo = "BILL-2026-04-004", Type = "electric", Description = "电费", Unit = "B栋1203", Amount = 450, BillingMonth = thisMonth, Status = "unpaid", ProjectId = 1, CreatedAt = thisMonth },
            new Bill { Id = 5, BillNo = "BILL-2026-03-001", Type = "property", Description = "物业费", Unit = "A栋501", Amount = 800, BillingMonth = lastMonth, Status = "paid", PaidAt = lastMonth.AddDays(15), TransactionId = "TXN-A003", ProjectId = 1, CreatedAt = lastMonth },
            new Bill { Id = 6, BillNo = "BILL-2026-03-002", Type = "water", Description = "水费", Unit = "B栋1203", Amount = 120, BillingMonth = lastMonth, Status = "paid", PaidAt = lastMonth.AddDays(20), TransactionId = "TXN-A004", ProjectId = 1, CreatedAt = lastMonth }
        );
    }
}