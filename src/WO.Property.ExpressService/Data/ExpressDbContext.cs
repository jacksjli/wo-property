using Microsoft.EntityFrameworkCore;
using WO.Property.ExpressService.Models;

namespace WO.Property.ExpressService.Data;

/// <summary>
/// 旧版 DbContext（保留用于数据初始化）
/// 当前使用 TenantDbContext
/// </summary>
public class ExpressDbContext : DbContext
{
    public ExpressDbContext(DbContextOptions<ExpressDbContext> options) : base(options) { }
    public DbSet<ExpressCompany> Companies { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<ExpressCompany>(e => { e.HasKey(x => x.Id); e.HasIndex(x => x.Name); });
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
    }
}
