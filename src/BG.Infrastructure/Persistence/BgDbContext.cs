using BG.Domain.Guarantees;
using BG.Domain.Identity;
using BG.Domain.Operations;
using BG.Domain.Workflow;
using BG.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

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

    public DbSet<Notification> Notifications => Set<Notification>();

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
        var existenceCache = new Dictionary<Type, PersistedEntityCache>();

        foreach (var guaranteeEntry in ChangeTracker.Entries<Guarantee>()
                     .Where(entry => entry.State is not EntityState.Detached and not EntityState.Deleted))
        {
            TrackAggregateChildren(guaranteeEntry.Entity.Requests, GuaranteeRequests, request => request.Id, existenceCache);
            TrackAggregateChildren(guaranteeEntry.Entity.Documents, GuaranteeDocuments, document => document.Id, existenceCache);
            TrackAggregateChildren(guaranteeEntry.Entity.Correspondence, GuaranteeCorrespondence, correspondence => correspondence.Id, existenceCache);
            TrackAggregateChildren(guaranteeEntry.Entity.Events, GuaranteeEvents, ledgerEvent => ledgerEvent.Id, existenceCache);
        }

        foreach (var requestEntry in ChangeTracker.Entries<GuaranteeRequest>()
                     .Where(entry => entry.State is not EntityState.Detached and not EntityState.Deleted))
        {
            TrackAggregateChildren(requestEntry.Entity.RequestDocuments, GuaranteeRequestDocuments, link => link.Id, existenceCache);
            TrackAggregateChildren(requestEntry.Entity.Correspondence, GuaranteeCorrespondence, correspondence => correspondence.Id, existenceCache);

            if (requestEntry.Entity.ApprovalProcess is not null)
            {
                TrackAggregateChildren([requestEntry.Entity.ApprovalProcess], RequestApprovalProcesses, process => process.Id, existenceCache);
                TrackAggregateChildren(requestEntry.Entity.ApprovalProcess.Stages, RequestApprovalStages, stage => stage.Id, existenceCache);
            }
        }
    }

    private void TrackAggregateChildren<T>(
        IEnumerable<T> entities,
        DbSet<T> dbSet,
        Expression<Func<T, Guid>> keySelectorExpression,
        IDictionary<Type, PersistedEntityCache> existenceCache)
        where T : class
    {
        var trackedEntries = entities
            .Select(entity => (Entity: entity, Entry: Entry(entity)))
            .ToArray();

        foreach (var tracked in trackedEntries.Where(tracked => tracked.Entry.State == EntityState.Detached))
        {
            tracked.Entry.State = EntityState.Added;
        }

        var persistedCandidates = trackedEntries
            .Where(tracked => tracked.Entry.State is EntityState.Unchanged or EntityState.Modified)
            .ToArray();

        if (persistedCandidates.Length == 0)
        {
            return;
        }

        var keySelector = keySelectorExpression.Compile();
        var persistedEntityCache = GetPersistedEntityCache(typeof(T), existenceCache);
        var candidateIds = persistedCandidates
            .Select(tracked => keySelector(tracked.Entity))
            .Distinct()
            .ToArray();

        foreach (var candidateId in candidateIds.Where(candidateId => candidateId == Guid.Empty))
        {
            persistedEntityCache.MissingIds.Add(candidateId);
        }

        var unknownCandidateIds = candidateIds
            .Where(candidateId =>
                candidateId != Guid.Empty &&
                !persistedEntityCache.ExistingIds.Contains(candidateId) &&
                !persistedEntityCache.MissingIds.Contains(candidateId))
            .ToArray();

        if (unknownCandidateIds.Length > 0)
        {
            var parameter = keySelectorExpression.Parameters[0];
            var containsExpression = Expression.Call(
                typeof(Enumerable),
                nameof(Enumerable.Contains),
                [typeof(Guid)],
                Expression.Constant(unknownCandidateIds),
                keySelectorExpression.Body);
            var predicate = Expression.Lambda<Func<T, bool>>(containsExpression, parameter);

            var existingIds = dbSet
                .AsNoTracking()
                .Where(predicate)
                .Select(keySelectorExpression)
                .ToHashSet();

            foreach (var existingId in existingIds)
            {
                persistedEntityCache.ExistingIds.Add(existingId);
            }

            foreach (var candidateId in unknownCandidateIds.Where(candidateId => !existingIds.Contains(candidateId)))
            {
                persistedEntityCache.MissingIds.Add(candidateId);
            }
        }

        foreach (var tracked in persistedCandidates.Where(tracked => persistedEntityCache.MissingIds.Contains(keySelector(tracked.Entity))))
        {
            tracked.Entry.State = EntityState.Added;
        }
    }

    private static PersistedEntityCache GetPersistedEntityCache(
        Type entityType,
        IDictionary<Type, PersistedEntityCache> existenceCache)
    {
        if (existenceCache.TryGetValue(entityType, out var cache))
        {
            return cache;
        }

        cache = new PersistedEntityCache();
        existenceCache[entityType] = cache;
        return cache;
    }

    private sealed class PersistedEntityCache
    {
        public HashSet<Guid> ExistingIds { get; } = [];

        public HashSet<Guid> MissingIds { get; } = [];
    }
}
