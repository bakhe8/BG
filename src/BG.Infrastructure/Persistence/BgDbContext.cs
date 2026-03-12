using BG.Domain.Guarantees;
using BG.Domain.Identity;
using BG.Domain.Operations;
using BG.Domain.Workflow;
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

    public DbSet<GuaranteeRequestDocumentLink> GuaranteeRequestDocuments => Set<GuaranteeRequestDocumentLink>();

    public DbSet<OperationsReviewItem> OperationsReviewItems => Set<OperationsReviewItem>();

    public DbSet<RequestWorkflowDefinition> RequestWorkflowDefinitions => Set<RequestWorkflowDefinition>();

    public DbSet<RequestWorkflowStageDefinition> RequestWorkflowStages => Set<RequestWorkflowStageDefinition>();

    public DbSet<ApprovalDelegation> ApprovalDelegations => Set<ApprovalDelegation>();

    public DbSet<RequestApprovalProcess> RequestApprovalProcesses => Set<RequestApprovalProcess>();

    public DbSet<RequestApprovalStage> RequestApprovalStages => Set<RequestApprovalStage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BgDbContext).Assembly);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        TrackPendingAggregateChildren();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        TrackPendingAggregateChildren();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void TrackPendingAggregateChildren()
    {
        foreach (var guaranteeEntry in ChangeTracker.Entries<Guarantee>()
                     .Where(entry => entry.State is not EntityState.Detached and not EntityState.Deleted))
        {
            TrackAddedEntities(guaranteeEntry.Entity.Requests);
            TrackAddedEntities(guaranteeEntry.Entity.Documents);
            TrackAddedEntities(guaranteeEntry.Entity.Correspondence);
            TrackAddedEntities(guaranteeEntry.Entity.Events);
        }

        foreach (var requestEntry in ChangeTracker.Entries<GuaranteeRequest>()
                     .Where(entry => entry.State is not EntityState.Detached and not EntityState.Deleted))
        {
            TrackAddedEntities(requestEntry.Entity.RequestDocuments);
            TrackAddedEntities(requestEntry.Entity.Correspondence);

            if (requestEntry.Entity.ApprovalProcess is not null)
            {
                TrackAddedEntity(requestEntry.Entity.ApprovalProcess);
                TrackAddedEntities(requestEntry.Entity.ApprovalProcess.Stages);
            }
        }
    }

    private void TrackAddedEntities<T>(IEnumerable<T> entities)
        where T : class
    {
        foreach (var entity in entities)
        {
            TrackAddedEntity(entity);
        }
    }

    private void TrackAddedEntity<T>(T entity)
        where T : class
    {
        var entry = Entry(entity);
        if (entry.State == EntityState.Detached)
        {
            entry.State = EntityState.Added;
        }
    }
}
