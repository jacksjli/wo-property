using Microsoft.EntityFrameworkCore;
using WO.Property.PersonService.Models;

namespace WO.Property.PersonService.Data;

public class TenantDbContext : DbContext
{
    private readonly Tenant.ITenantDbFactory _tenantDbFactory;

    public DbSet<Personnel> Personnel => Set<Personnel>();

    public TenantDbContext(
        DbContextOptions<TenantDbContext> options,
        Tenant.ITenantDbFactory tenantDbFactory,
        ILogger<TenantDbContext> logger)
        : base(options)
    {
        _tenantDbFactory = tenantDbFactory;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Personnel>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("personnel");

            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.EmployeeNo).HasColumnName("EmployeeNo").HasMaxLength(30);
            entity.Property(e => e.Name).HasColumnName("Name").HasMaxLength(50);
            entity.Property(e => e.Avatar).HasColumnName("Avatar").HasMaxLength(255);
            entity.Property(e => e.Gender).HasColumnName("Gender").HasMaxLength(20);
            entity.Property(e => e.Birthday).HasColumnName("Birthday");
            entity.Property(e => e.IdCard).HasColumnName("IdCard").HasMaxLength(30);
            entity.Property(e => e.Phone).HasColumnName("Phone").HasMaxLength(20);
            entity.Property(e => e.Email).HasColumnName("Email").HasMaxLength(100);
            entity.Property(e => e.Address).HasColumnName("Address").HasMaxLength(200);
            entity.Property(e => e.Education).HasColumnName("Education").HasMaxLength(20);
            entity.Property(e => e.GraduateSchool).HasColumnName("GraduateSchool").HasMaxLength(100);
            entity.Property(e => e.Major).HasColumnName("Major").HasMaxLength(50);
            entity.Property(e => e.Role).HasColumnName("Role").HasMaxLength(30);
            entity.Property(e => e.DepartmentId).HasColumnName("DepartmentId");
            entity.Property(e => e.DepartmentName).HasColumnName("DepartmentName").HasMaxLength(50);
            entity.Property(e => e.Position).HasColumnName("Position").HasMaxLength(50);
            entity.Property(e => e.EmploymentType).HasColumnName("EmploymentType").HasMaxLength(20);
            entity.Property(e => e.HireDate).HasColumnName("HireDate");
            entity.Property(e => e.ContractStart).HasColumnName("ContractStart");
            entity.Property(e => e.ContractEnd).HasColumnName("ContractEnd");
            entity.Property(e => e.Salary).HasColumnName("Salary").HasColumnType("decimal(12,2)");
            entity.Property(e => e.BankAccount).HasColumnName("BankAccount").HasMaxLength(50);
            entity.Property(e => e.SocialSecurityNo).HasColumnName("SocialSecurityNo").HasMaxLength(50);
            entity.Property(e => e.Status).HasColumnName("Status").HasMaxLength(20);
            entity.Property(e => e.Specialties).HasColumnName("Specialties");
            entity.Property(e => e.Backups).HasColumnName("Backups");
            entity.Property(e => e.EmergencyContactName).HasColumnName("EmergencyContactName").HasMaxLength(50);
            entity.Property(e => e.EmergencyContactRelationship).HasColumnName("EmergencyContactRelationship").HasMaxLength(20);
            entity.Property(e => e.EmergencyContactPhone).HasColumnName("EmergencyContactPhone").HasMaxLength(20);
            entity.Property(e => e.AttendanceCount).HasColumnName("AttendanceCount");
            entity.Property(e => e.OvertimeHours).HasColumnName("OvertimeHours").HasColumnType("decimal(5,2)");
            entity.Property(e => e.LeaveDays).HasColumnName("LeaveDays");
            entity.Property(e => e.PerformanceScore).HasColumnName("PerformanceScore").HasColumnType("decimal(3,1)");
            entity.Property(e => e.TrainingCount).HasColumnName("TrainingCount");
            entity.Property(e => e.Remark).HasColumnName("Remark");
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt");
            entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt");
            entity.Property(e => e.TicketTypeIds).HasColumnName("ticket_type_ids");
            entity.Property(e => e.SpecialtyIds).HasColumnName("specialty_ids");
            entity.Property(e => e.AreaIds).HasColumnName("area_ids");
            entity.Property(e => e.IsSupervisor).HasColumnName("is_supervisor");
            entity.Property(e => e.MaxConcurrentTickets).HasColumnName("max_concurrent_tickets");
        });
    }
}