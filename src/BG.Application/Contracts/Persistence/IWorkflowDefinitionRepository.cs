using BG.Domain.Guarantees;
using BG.Domain.Identity;
using BG.Domain.Workflow;

namespace BG.Application.Contracts.Persistence;

public interface IWorkflowDefinitionRepository
{
    Task<IReadOnlyList<RequestWorkflowDefinition>> ListDefinitionsAsync(CancellationToken cancellationToken = default);

    Task<RequestWorkflowDefinition?> GetDefinitionByIdAsync(Guid definitionId, CancellationToken cancellationToken = default);

    Task<RequestWorkflowDefinition?> GetDefinitionAsync(
        GuaranteeRequestType requestType,
        GuaranteeCategory? guaranteeCategory,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Role>> ListRolesAsync(CancellationToken cancellationToken = default);

    Task<Role?> GetRoleByIdAsync(Guid roleId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
