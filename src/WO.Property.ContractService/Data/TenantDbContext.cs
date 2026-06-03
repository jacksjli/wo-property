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
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.ContractNumber).HasColumnName("ContractNumber");
            entity.Property(e => e.Type).HasColumnName("ContractType").IsRequired(false);
            entity.Property(e => e.Title).HasColumnName("ContractName");
            entity.Property(e => e.Description).HasColumnName("Remarks").HasColumnType("text").IsRequired(false);
            entity.Property(e => e.PartyA).HasColumnName("PartyA").IsRequired(false);
            entity.Property(e => e.PartyB).HasColumnName("PartyB").IsRequired(false);
            entity.Property(e => e.Amount).HasColumnName("Amount").HasColumnType("decimal(18,2)").IsRequired(false);
            entity.Property(e => e.StartDate).HasColumnName("StartDate").IsRequired(false);
            entity.Property(e => e.EndDate).HasColumnName("EndDate").IsRequired(false);
            entity.Property(e => e.AttachmentUrl).HasColumnName("AttachmentUrl").IsRequired(false);
            entity.Property(e => e.Status).HasColumnName("Status").IsRequired(false);
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Ignore(e => e.PartyAContact);
            entity.Ignore(e => e.PartyAPhone);
            entity.Ignore(e => e.PartyBContact);
            entity.Ignore(e => e.PartyBPhone);
            entity.Ignore(e => e.Currency);
            entity.Ignore(e => e.ExtendedData);
            entity.Ignore(e => e.CreatedBy);
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.UpdatedAt);
            entity.Ignore(e => e.IsDeleted);
            entity.Property(e => e.ProjectCode).HasColumnName("project_code");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("payments");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ContractId).HasColumnName("contract_id");
            entity.Property(e => e.PaymentNumber).HasColumnName("payment_number");
            entity.Property(e => e.Description).HasColumnName("description").IsRequired(false);
            entity.Property(e => e.Amount).HasColumnName("amount").HasColumnType("decimal(18,2)").IsRequired(false);
            entity.Property(e => e.Currency).HasColumnName("currency").IsRequired(false);
            entity.Property(e => e.DueDate).HasColumnName("due_date").IsRequired(false);
            entity.Property(e => e.PaidDate).HasColumnName("paid_date").IsRequired(false);
            entity.Property(e => e.Status).HasColumnName("status").IsRequired(false);
            entity.Property(e => e.CreatedBy).HasColumnName("created_by").IsRequired(false);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
            entity.Ignore(e => e.Remarks);
            entity.Ignore(e => e.Contract);
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.UpdatedAt);
            entity.Ignore(e => e.IsDeleted);
        });
    }
}