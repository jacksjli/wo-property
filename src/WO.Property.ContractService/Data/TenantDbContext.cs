using Microsoft.EntityFrameworkCore;
using WO.Property.ContractService.Models;

namespace WO.Property.ContractService.Data;

public class TenantDbContext : DbContext
{
    private readonly Tenant.ITenantDbFactory _tenantDbFactory;
    private readonly ILogger<TenantDbContext> _logger;

    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<Payment> Payments => Set<Payment>();

    public TenantDbContext(DbContextOptions<TenantDbContext> options, Tenant.ITenantDbFactory tenantDbFactory, ILogger<TenantDbContext> logger)
        : base(options)
    {
        _tenantDbFactory = tenantDbFactory;
        _logger = logger;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Contract>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("contracts");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ContractNumber).HasColumnName("contract_number");
            entity.Property(e => e.Type).HasColumnName("type").HasConversion<string>();
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.PartyA).HasColumnName("party_a");
            entity.Property(e => e.PartyAContact).HasColumnName("party_a_contact");
            entity.Property(e => e.PartyAPhone).HasColumnName("party_a_phone");
            entity.Property(e => e.PartyB).HasColumnName("party_b");
            entity.Property(e => e.PartyBContact).HasColumnName("party_b_contact");
            entity.Property(e => e.PartyBPhone).HasColumnName("party_b_phone");
            entity.Property(e => e.Amount).HasColumnName("amount").HasColumnType("decimal(18,2)");
            entity.Property(e => e.Currency).HasColumnName("currency");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.AttachmentUrl).HasColumnName("attachment_url");
            entity.Property(e => e.Status).HasColumnName("status").HasConversion<string>();
            entity.Property(e => e.ExtendedData).HasColumnName("extended_data");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.UpdatedAt);
            entity.Ignore(e => e.IsDeleted);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("payments");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ContractId).HasColumnName("contract_id");
            entity.Property(e => e.PaymentNumber).HasColumnName("payment_number");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Amount).HasColumnName("amount").HasColumnType("decimal(18,2)");
            entity.Property(e => e.Currency).HasColumnName("currency");
            entity.Property(e => e.DueDate).HasColumnName("due_date");
            entity.Property(e => e.PaidDate).HasColumnName("paid_date");
            entity.Property(e => e.Status).HasColumnName("status").HasConversion<string>();
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.UpdatedAt);
            entity.Ignore(e => e.IsDeleted);
        });
    }
}