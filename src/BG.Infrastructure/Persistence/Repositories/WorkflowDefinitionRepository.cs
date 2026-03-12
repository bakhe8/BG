using BG.Application.Contracts.Persistence;
using BG.Domain.Guarantees;
using BG.Domain.Identity;
using BG.Domain.Workflow;
using Microsoft.EntityFrameworkCore;

namespace BG.Infrastructure.Persistence.Repositories;

internal sealed class WorkflowDefinitionRepository : IWorkflowDefinitionRepository
{
    private readonly BgDbContext _dbContext;

    public WorkflowDefinitionRepository(BgDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<RequestWorkflowDefinition>> ListDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.RequestWorkflowDefinitions
            .Include(definition => definition.Stages)
            .ThenInclude(stage => stage.Role)
            .OrderBy(definition => definition.RequestType)
            .ThenBy(definition => definition.GuaranteeCategory)
            .ToListAsync(cancellationToken);
    }

    public async Task<RequestWorkflowDefinition?> GetDefinitionByIdAsync(Guid definitionId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.RequestWorkflowDefinitions
            .Include(definition => definition.Stages)
            .ThenInclude(stage => stage.Role)
            .SingleOrDefaultAsync(definition => definition.Id == definitionId, cancellationToken);
    }

    public async Task<RequestWorkflowDefinition?> GetDefinitionAsync(
        GuaranteeRequestType requestType,
        GuaranteeCategory? guaranteeCategory,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.RequestWorkflowDefinitions
            .AsNoTracking()
            .Include(definition => definition.Stages)
            .ThenInclude(stage => stage.Role)
            .SingleOrDefaultAsync(
                definition => definition.RequestType == requestType &&
                              definition.GuaranteeCategory == guaranteeCategory &&
                              definition.IsActive,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> ListRolesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Roles
            .AsNoTracking()
            .OrderBy(role => role.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<Role?> GetRoleByIdAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Roles
            .SingleOrDefaultAsync(role => role.Id == roleId, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
