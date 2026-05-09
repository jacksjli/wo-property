using Microsoft.EntityFrameworkCore;
using WO.Property.ProjectConfigService.Models;

namespace WO.Property.ProjectConfigService.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectConfig> ProjectConfigs => Set<ProjectConfig>();
    public DbSet<GlobalConfig> GlobalConfigs => Set<GlobalConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Project>(e =>
        {
            e.HasIndex(p => p.Code).IsUnique();
            e.HasIndex(p => p.Status);
        });

        modelBuilder.Entity<ProjectConfig>(e =>
        {
            e.HasIndex(c => new { c.ProjectId, c.Module, c.ConfigKey }).IsUnique();
            e.HasIndex(c => c.Module);
        });

        modelBuilder.Entity<GlobalConfig>(e =>
        {
            e.HasIndex(c => c.ConfigKey).IsUnique();
        });
    }
}
