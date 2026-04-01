using BG.Application.Models.Intake;

namespace BG.Application.Contracts.Services;

public interface IIntakeDocumentStore
{
    Task<StagedIntakeDocumentDto> StageAsync(
        string originalFileName,
        Stream content,
        CancellationToken cancellationToken = default);

    Task<PromotedIntakeDocumentDto> PromoteAsync(
        string stagedDocumentToken,
        string guaranteeNumber,
        CancellationToken cancellationToken = default);
}
