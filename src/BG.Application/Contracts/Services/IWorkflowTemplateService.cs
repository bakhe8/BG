using BG.Application.Operations;
using BG.Domain.Guarantees;

namespace BG.Application.Contracts.Services;

public interface IWorkflowTemplateService
{
    Task<IReadOnlyList<RequestWorkflowTemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken = default);

    Task<RequestWorkflowTemplateDto?> GetTemplateAsync(
        GuaranteeRequestType requestType,
        GuaranteeCategory? guaranteeCategory,
        CancellationToken cancellationToken = default);
}
