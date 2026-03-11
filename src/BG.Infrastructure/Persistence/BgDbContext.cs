using BG.Domain.Guarantees;
using BG.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace BG.Infrastructure.Persistence;

public sealed class BgDbContext : DbContext
{
    public BgDbContext(DbContextOptions<BgDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<Guarantee> Guarantees => Set<Guarantee>();

    public DbSet<GuaranteeRequest> GuaranteeRequests => Set<GuaranteeRequest>();

    public DbSet<GuaranteeCorrespondence> GuaranteeCorrespondence => Set<GuaranteeCorrespondence>();

    public DbSet<GuaranteeDocument> GuaranteeDocuments => Set<GuaranteeDocument>();

    public DbSet<GuaranteeEvent> GuaranteeEvents => Set<GuaranteeEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BgDbContext).Assembly);
    }
}
