using Microsoft.EntityFrameworkCore;
using WO.Property.CenterService.Models;

namespace WO.Property.CenterService.Data;

public class CenterDbContext : DbContext
{
    public CenterDbContext(DbContextOptions<CenterDbContext> options) : base(options)
    {
    }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<User> Users => Set<User>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<ProjectModule> ProjectModules => Set<ProjectModule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Project - matches ACTUAL database schema
        modelBuilder.Entity<Project>(entity =>
        {
            entity.ToTable("projects");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code).HasColumnName("project_code").HasMaxLength(50);
            entity.Property(e => e.Name).HasColumnName("project_name").HasMaxLength(100);
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.Address).HasColumnName("address").HasMaxLength(200);
            entity.Property(e => e.DatabaseName).HasColumnName("database_name").HasMaxLength(50);
            entity.Property(e => e.Description).HasColumnName("description").HasColumnType("text");
            entity.Property(e => e.ContactPhone).HasColumnName("contact_phone").HasMaxLength(20);
            entity.Property(e => e.Config).HasColumnName("config").HasColumnType("json");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => e.Code).IsUnique();
        });

        // User - matches actual database schema
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Username).HasColumnName("username").HasMaxLength(50);
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash").HasMaxLength(255);
            entity.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(100);
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(100);
            entity.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(20);
            entity.Property(e => e.Avatar).HasColumnName("avatar").HasMaxLength(255);
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.LastLoginAt).HasColumnName("last_login_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => e.Username).IsUnique();
        });

        // ProjectMember - matches actual database schema
        modelBuilder.Entity<ProjectMember>(entity =>
        {
            entity.ToTable("project_members");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ProjectCode).HasColumnName("project_code").HasMaxLength(50);
            entity.Property(e => e.Role).HasColumnName("role");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => new { e.UserId, e.ProjectCode }).IsUnique();
        });

        // ProjectModule - matches ACTUAL database schema (id, project_id, module_code, enabled)
        modelBuilder.Entity<ProjectModule>(entity =>
        {
            entity.ToTable("project_modules");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.ModuleKey).HasColumnName("module_code").HasMaxLength(50);
            // Note: actual table has 'enabled' column, not config/sort_order/status/created_at/updated_at
            entity.Property(e => e.Enabled).HasColumnName("enabled");

            entity.HasIndex(e => new { e.ProjectId, e.ModuleKey }).IsUnique();
        });
    }
}