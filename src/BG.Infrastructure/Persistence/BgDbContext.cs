using BG.Domain.Guarantees;
using BG.Domain.Identity;
using BG.Domain.Operations;
using BG.Domain.Workflow;
using BG.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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

    public DbSet<Bank> Banks => Set<Bank>();

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
        var candidatesByType = new Dictionary<Type, List<(object Entity, Guid Id, EntityEntry Entry)>>();

        // 1. Collect all detached or potentially untracked children from all relevant aggregates
        foreach (var guaranteeEntry in ChangeTracker.Entries<Guarantee>()
                     .Where(entry => entry.State is not EntityState.Detached and not EntityState.Deleted))
        {
            CollectCandidates(guaranteeEntry.Entity.Requests, request => request.Id, candidatesByType);
            CollectCandidates(guaranteeEntry.Entity.Documents, document => document.Id, candidatesByType);
            CollectCandidates(guaranteeEntry.Entity.Correspondence, correspondence => correspondence.Id, candidatesByType);
            CollectCandidates(guaranteeEntry.Entity.Events, ledgerEvent => ledgerEvent.Id, candidatesByType);
        }

        foreach (var requestEntry in ChangeTracker.Entries<GuaranteeRequest>()
                     .Where(entry => entry.State is not EntityState.Detached and not EntityState.Deleted))
        {
            CollectCandidates(requestEntry.Entity.RequestDocuments, link => link.Id, candidatesByType);
            CollectCandidates(requestEntry.Entity.Correspondence, correspondence => correspondence.Id, candidatesByType);

            if (requestEntry.Entity.ApprovalProcess is not null)
            {
                CollectCandidates([requestEntry.Entity.ApprovalProcess], process => process.Id, candidatesByType);
                CollectCandidates(requestEntry.Entity.ApprovalProcess.Stages, stage => stage.Id, candidatesByType);
            }
        }

        if (candidatesByType.Count == 0) return;

        // 2. Process each type in a batched manner
        foreach (var (type, candidates) in candidatesByType)
        {
            ProcessTypeBatch(type, candidates);
        }
    }

    private void CollectCandidates<T>(IEnumerable<T> entities, Func<T, Guid> idSelector, Dictionary<Type, List<(object Entity, Guid Id, EntityEntry Entry)>> candidatesByType) where T : class
    {
        var type = typeof(T);
        if (!candidatesByType.TryGetValue(type, out var list))
        {
            list = new List<(object Entity, Guid Id, EntityEntry Entry)>();
            candidatesByType[type] = list;
        }

        foreach (var entity in entities)
        {
            var entry = Entry(entity);
            if (entry.State == EntityState.Detached)
            {
                entry.State = EntityState.Added;
                continue;
            }

            if (entry.State is EntityState.Unchanged or EntityState.Modified)
            {
                list.Add((entity, idSelector(entity), entry));
            }
        }
    }

    private void ProcessTypeBatch(Type entityType, List<(object Entity, Guid Id, EntityEntry Entry)> candidates)
    {
        if (candidates.Count == 0) return;

        var unknownIds = candidates
            .Select(c => c.Id)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        if (unknownIds.Length == 0)
        {
            // If all are Guid.Empty, they are all Missing (Added)
            foreach (var candidate in candidates) candidate.Entry.State = EntityState.Added;
            return;
        }

        // Perform ONE query for the unknown IDs of this type
        var dbSet = GetDbSet(entityType);
        var parameter = Expression.Parameter(entityType, "e");
        var property = Expression.Property(parameter, "Id");
        var containsMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(Guid));
        var body = Expression.Call(null, containsMethod, Expression.Constant(unknownIds), property);
        var predicate = Expression.Lambda(body, parameter);

        var query = (IQueryable)dbSet.GetType().GetMethod("AsNoTracking")!.Invoke(dbSet, null)!;
        var whereMethod = typeof(Queryable).GetMethods()
            .First(m => m.Name == "Where" && m.GetParameters()[1].ParameterType.GetGenericArguments()[0].GetGenericArguments().Length == 2)
            .MakeGenericMethod(entityType);
        query = (IQueryable)whereMethod.Invoke(null, [query, predicate])!;

        var selectMethod = typeof(Queryable).GetMethods()
            .First(m => m.Name == "Select" && m.GetParameters()[1].ParameterType.GetGenericArguments()[0].GetGenericArguments().Length == 2)
            .MakeGenericMethod(entityType, typeof(Guid));
        var selectLambda = Expression.Lambda(property, parameter);
        var existingIdsQuery = (IQueryable<Guid>)selectMethod.Invoke(null, [query, selectLambda])!;

        var existingIds = existingIdsQuery.ToHashSet();

        foreach (var candidate in candidates)
        {
            if (candidate.Id == Guid.Empty || !existingIds.Contains(candidate.Id))
            {
                candidate.Entry.State = EntityState.Added;
            }
        }
    }

    private object GetDbSet(Type type)
    {
        return GetType().GetMethods()
            .First(m => m.Name == "Set" && m.IsGenericMethod && m.GetParameters().Length == 0)
            .MakeGenericMethod(type)
            .Invoke(this, null)!;
    }
}
