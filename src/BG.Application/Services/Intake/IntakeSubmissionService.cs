using System.Globalization;
using System.Text.Json;
using BG.Application.Common;
using BG.Application.Contracts.Persistence;
using BG.Application.Contracts.Services;
using BG.Application.Intake;
using BG.Application.Models.Intake;
using BG.Application.ReferenceData;
using BG.Domain.Guarantees;
using BG.Domain.Operations;

namespace BG.Application.Services;

internal sealed class IntakeSubmissionService : IIntakeSubmissionService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly IIntakeRepository _repository;
    private readonly IIntakeDocumentStore _documentStore;
    private readonly IIntakeExtractionEngine _extractionEngine;
    private readonly INotificationService _notificationService;

    public IntakeSubmissionService(
        IIntakeRepository repository,
        IIntakeDocumentStore documentStore,
        IIntakeExtractionEngine extractionEngine,
        INotificationService notificationService)
    {
        _repository = repository;
        _documentStore = documentStore;
        _extractionEngine = extractionEngine;
        _notificationService = notificationService;
    }

    public async Task<OperationResult<IntakeExtractionDraftDto>> BeginExtractionAsync(
        BeginIntakeExtractionCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.ScenarioKey))
        {
            return OperationResult<IntakeExtractionDraftDto>.Failure(IntakeErrorCodes.ScenarioRequired);
        }

        if (IntakeScenarioCatalog.Find(command.ScenarioKey) is null)
        {
            return OperationResult<IntakeExtractionDraftDto>.Failure(IntakeErrorCodes.UnsupportedScenario);
        }

        if (string.IsNullOrWhiteSpace(command.FileName))
        {
            return OperationResult<IntakeExtractionDraftDto>.Failure(IntakeErrorCodes.FileRequired);
        }

        var stagedDocument = await _documentStore.StageAsync(command.FileName, command.Content, cancellationToken);

        if (stagedDocument.FileSize <= 0)
        {
            return OperationResult<IntakeExtractionDraftDto>.Failure(IntakeErrorCodes.EmptyFile);
        }

        var draft = await _extractionEngine.ExtractAsync(command.ScenarioKey, stagedDocument, cancellationToken);
        return OperationResult<IntakeExtractionDraftDto>.Success(draft);
    }

    public async Task<OperationResult<IntakeSubmissionReceiptDto>> FinalizeAsync(
        IntakeSubmissionCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.IntakeActorUserId == Guid.Empty)
        {
            return OperationResult<IntakeSubmissionReceiptDto>.Failure(IntakeErrorCodes.IntakeActorRequired);
        }

        var intakeActor = await _repository.GetIntakeActorByIdAsync(command.IntakeActorUserId, cancellationToken);
        if (intakeActor is null)
        {
            return OperationResult<IntakeSubmissionReceiptDto>.Failure(IntakeErrorCodes.IntakeActorInvalid);
        }

        if (string.IsNullOrWhiteSpace(command.ScenarioKey))
        {
            return OperationResult<IntakeSubmissionReceiptDto>.Failure(IntakeErrorCodes.ScenarioRequired);
        }

        var scenario = IntakeScenarioCatalog.Find(command.ScenarioKey);

        if (scenario is null)
        {
            return OperationResult<IntakeSubmissionReceiptDto>.Failure(IntakeErrorCodes.UnsupportedScenario);
        }

        if (!GuaranteeDocumentFormCatalog.IsSupportedForScenario(command.ScenarioKey, command.DocumentFormKey))
        {
            return OperationResult<IntakeSubmissionReceiptDto>.Failure(IntakeErrorCodes.DocumentFormInvalid);
        }

        if (string.IsNullOrWhiteSpace(command.StagedDocumentToken))
        {
            return OperationResult<IntakeSubmissionReceiptDto>.Failure(IntakeErrorCodes.StagedDocumentNotFound);
        }

        var sourceSystemName = Normalize(command.SourceSystemName);
        var sourceReference = Normalize(command.SourceReference);

        if (command.CaptureChannel is GuaranteeDocumentCaptureChannel.ScanStation or GuaranteeDocumentCaptureChannel.OracleImport &&
            string.IsNullOrWhiteSpace(sourceSystemName))
        {
            return OperationResult<IntakeSubmissionReceiptDto>.Failure(IntakeErrorCodes.SourceSystemNameRequired);
        }

        if (command.CaptureChannel == GuaranteeDocumentCaptureChannel.OracleImport &&
            string.IsNullOrWhiteSpace(sourceReference))
        {
            return OperationResult<IntakeSubmissionReceiptDto>.Failure(IntakeErrorCodes.SourceReferenceRequired);
        }

        var guaranteeNumber = Normalize(command.GuaranteeNumber);

        if (string.IsNullOrWhiteSpace(guaranteeNumber))
        {
            return OperationResult<IntakeSubmissionReceiptDto>.Failure(IntakeErrorCodes.GuaranteeNumberRequired);
        }

        if (!scenario.RequiresExistingGuarantee)
        {
            return await FinalizeNewGuaranteeAsync(scenario, guaranteeNumber, intakeActor, command, cancellationToken);
        }

        return await FinalizeExistingGuaranteeAsync(scenario, guaranteeNumber, intakeActor, command, cancellationToken);
    }

    private async Task<OperationResult<IntakeSubmissionReceiptDto>> FinalizeNewGuaranteeAsync(
        IntakeScenarioDefinition scenario,
        string guaranteeNumber,
        BG.Domain.Identity.User intakeActor,
        IntakeSubmissionCommand command,
        CancellationToken cancellationToken)
    {
        var documentForm = GuaranteeDocumentFormCatalog.Find(command.DocumentFormKey)
            ?? throw new InvalidOperationException($"Unsupported document form '{command.DocumentFormKey}'.");

        if (await _repository.GuaranteeNumberExistsAsync(guaranteeNumber, cancellationToken))
        {
            return OperationResult<IntakeSubmissionReceiptDto>.Failure(IntakeErrorCodes.DuplicateGuaranteeNumber);
        }

        var bankName = Normalize(command.BankName) ?? documentForm.CanonicalBankName;
        var beneficiary = Normalize(command.BeneficiaryName);
        var principal = Normalize(command.PrincipalName);
        var currencyCode = Normalize(command.CurrencyCode)?.ToUpperInvariant();
        var guaranteeCategory = command.GuaranteeCategory;

        if (string.IsNullOrWhiteSpace(bankName))
        {
            return OperationResult<IntakeSubmissionReceiptDto>.Failure(IntakeErrorCodes.BankNameRequired);
        }

        if (string.IsNullOrWhiteSpace(beneficiary))
        {
            return OperationResult<IntakeSubmissionReceiptDto>.Failure(IntakeErrorCodes.BeneficiaryRequired);
        }

        if (string.IsNullOrWhiteSpace(principal))
        {
            return OperationResult<IntakeSubmissionReceiptDto>.Failure(IntakeErrorCodes.PrincipalRequired);
        }

        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            return OperationResult<IntakeSubmissionReceiptDto>.Failure(IntakeErrorCodes.CurrencyRequired);
        }

        if (guaranteeCategory is null || !Enum.IsDefined(guaranteeCategory.Value))
        {
            return OperationResult<IntakeSubmissionReceiptDto>.Failure(IntakeErrorCodes.GuaranteeCategoryRequired);
        }

        if (!TryParseAmount(command.Amount, out var amount))
        {
            return OperationResult<IntakeSubmissionReceiptDto>.Failure(IntakeErrorCodes.AmountInvalid);
        }

        if (!TryParseDate(command.IssueDate, out var issueDate))
        {
            return OperationResult<IntakeSubmissionReceiptDto>.Failure(IntakeErrorCodes.IssueDateRequired);
        }

        if (!TryParseDate(command.ExpiryDate, out var expiryDate))
        {
            return OperationResult<IntakeSubmissionReceiptDto>.Failure(IntakeErrorCodes.ExpiryDateRequired);
        }

        if (expiryDate < issueDate)
        {
            return OperationResult<IntakeSubmissionReceiptDto>.Failure(IntakeErrorCodes.ExpiryDateBeforeIssueDate);
        }

        var guarantee = Guarantee.RegisterNew(
            guaranteeNumber,
            bankName,
            beneficiary,
            principal,
            guaranteeCategory.Value,
            amount,
            currencyCode,
            issueDate,
            expiryDate,
            DateTimeOffset.UtcNow);

        var promotedDocument = await PromoteDocumentAsync(command, guaranteeNumber, cancellationToken);

        if (promotedDocument is null)
        {
            return OperationResult<IntakeSubmissionReceiptDto>.Failure(IntakeErrorCodes.StagedDocumentNotFound);
        }

        var document = guarantee.RegisterScannedDocument(
            scenario.DocumentType,
            promotedDocument.OriginalFileName,
            promotedDocument.StoragePath,
            Math.Max(command.PageCount, 1),
            DateTimeOffset.UtcNow,
            intakeActor.Id,
            intakeActor.DisplayName,
            command.CaptureChannel,
            Normalize(command.SourceSystemName),
            Normalize(command.SourceReference),
            scenario.Key,
            command.ExtractionRouteResourceKey,
            SerializeVerifiedData(scenario.Key, command),
            notes: null);

        await _repository.AddGuaranteeAsync(guarantee, cancellationToken);
        await _repository.AddOperationsReviewItemAsync(
            CreateOperationsReviewItem(
                guarantee,
                document,
                correspondenceId: null,
                scenario),
            cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        await _notificationService.SendNotificationAsync(
            $"New guarantee registration request #{guarantee.GuaranteeNumber} waiting for review.",
            $"/Operations/Queue",
            "permission.operations.queue",
            null,
            cancellationToken);

        return OperationResult<IntakeSubmissionReceiptDto>.Success(
            new IntakeSubmissionReceiptDto(
                guarantee.Id,
                guarantee.GuaranteeNumber,
                document.Id,
                scenario.Key,
                scenario.HandoffResourceKey));
    }

    private async Task<OperationResult<IntakeSubmissionReceiptDto>> FinalizeExistingGuaranteeAsync(
        IntakeScenarioDefinition scenario,
        string guaranteeNumber,
        BG.Domain.Identity.User intakeActor,
        IntakeSubmissionCommand command,
        CancellationToken cancellationToken)
    {
        _ = GuaranteeDocumentFormCatalog.Find(command.DocumentFormKey)
            ?? throw new InvalidOperationException($"Unsupported document form '{command.DocumentFormKey}'.");

        var guarantee = await _repository.GetGuaranteeByNumberAsync(guaranteeNumber, cancellationToken);

        if (guarantee is null)
        {
            return OperationResult<IntakeSubmissionReceiptDto>.Failure(IntakeErrorCodes.GuaranteeNotFound);
        }

        DateOnly? letterDate = null;
        string? bankReference = null;

        if (scenario.CorrespondenceKind is not null)
        {
            if (!TryParseDate(command.OfficialLetterDate, out var parsedLetterDate))
            {
                return OperationResult<IntakeSubmissionReceiptDto>.Failure(IntakeErrorCodes.LetterDateRequired);
            }

            bankReference = Normalize(command.BankReference);

            if (string.IsNullOrWhiteSpace(bankReference))
            {
                return OperationResult<IntakeSubmissionReceiptDto>.Failure(IntakeErrorCodes.BankReferenceRequired);
            }

            letterDate = parsedLetterDate;

            if (scenario.RequiresConfirmedExpiryDate && !TryParseDate(command.NewExpiryDate, out _))
            {
                return OperationResult<IntakeSubmissionReceiptDto>.Failure(IntakeErrorCodes.NewExpiryDateRequired);
            }

            if (scenario.RequiresConfirmedAmount && !TryParseAmount(command.Amount, out _))
            {
                return OperationResult<IntakeSubmissionReceiptDto>.Failure(IntakeErrorCodes.AmountInvalid);
            }

            if (scenario.RequiresStatusStatement && string.IsNullOrWhiteSpace(Normalize(command.StatusStatement)))
            {
                return OperationResult<IntakeSubmissionReceiptDto>.Failure(IntakeErrorCodes.StatusStatementRequired);
            }
        }
        else if (scenario.RequiresAttachmentNote && string.IsNullOrWhiteSpace(Normalize(command.AttachmentNote)))
        {
            return OperationResult<IntakeSubmissionReceiptDto>.Failure(IntakeErrorCodes.AttachmentNoteRequired);
        }

        var promotedDocument = await PromoteDocumentAsync(command, guaranteeNumber, cancellationToken);

        if (promotedDocument is null)
        {
            return OperationResult<IntakeSubmissionReceiptDto>.Failure(IntakeErrorCodes.StagedDocumentNotFound);
        }

        var note = scenario.BuildDocumentNote(command);
        var document = guarantee.RegisterScannedDocument(
            scenario.DocumentType,
            promotedDocument.OriginalFileName,
            promotedDocument.StoragePath,
            Math.Max(command.PageCount, 1),
            DateTimeOffset.UtcNow,
            intakeActor.Id,
            intakeActor.DisplayName,
            command.CaptureChannel,
            Normalize(command.SourceSystemName),
            Normalize(command.SourceReference),
            scenario.Key,
            command.ExtractionRouteResourceKey,
            SerializeVerifiedData(scenario.Key, command),
            note);

        Guid? correspondenceId = null;

        if (scenario.CorrespondenceKind is not null)
        {
            var correspondence = guarantee.RegisterCorrespondence(
                requestId: null,
                GuaranteeCorrespondenceDirection.Incoming,
                scenario.CorrespondenceKind.Value,
                bankReference!,
                letterDate!.Value,
                document.Id,
                note,
                DateTimeOffset.UtcNow,
                intakeActor.Id,
                intakeActor.DisplayName);

            correspondenceId = correspondence.Id;
        }

        await _repository.AddOperationsReviewItemAsync(
            CreateOperationsReviewItem(
                guarantee,
                document,
                correspondenceId,
                scenario),
            cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        await _notificationService.SendNotificationAsync(
            $"New correspondence/guarantee request #{guarantee.GuaranteeNumber} waiting for review.",
            $"/Operations/Queue",
            "permission.operations.queue",
            null,
            cancellationToken);

        return OperationResult<IntakeSubmissionReceiptDto>.Success(
            new IntakeSubmissionReceiptDto(
                guarantee.Id,
                guarantee.GuaranteeNumber,
                document.Id,
                scenario.Key,
                scenario.HandoffResourceKey));
    }

    private async Task<PromotedIntakeDocumentDto?> PromoteDocumentAsync(
        IntakeSubmissionCommand command,
        string guaranteeNumber,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _documentStore.PromoteAsync(command.StagedDocumentToken, guaranteeNumber, cancellationToken);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (DirectoryNotFoundException)
        {
            return null;
        }
    }

    private static string SerializeVerifiedData(string scenarioKey, IntakeSubmissionCommand command)
    {
        var payload = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [IntakeVerifiedDataKeys.ScenarioKey] = scenarioKey,
            [IntakeVerifiedDataKeys.DocumentFormKey] = Normalize(command.DocumentFormKey),
            [IntakeVerifiedDataKeys.GuaranteeNumber] = Normalize(command.GuaranteeNumber),
            [IntakeVerifiedDataKeys.BankName] = Normalize(command.BankName),
            [IntakeVerifiedDataKeys.BeneficiaryName] = Normalize(command.BeneficiaryName),
            [IntakeVerifiedDataKeys.PrincipalName] = Normalize(command.PrincipalName),
            [IntakeVerifiedDataKeys.GuaranteeCategory] = command.GuaranteeCategory?.ToString(),
            [IntakeVerifiedDataKeys.Amount] = Normalize(command.Amount),
            [IntakeVerifiedDataKeys.CurrencyCode] = Normalize(command.CurrencyCode),
            [IntakeVerifiedDataKeys.IssueDate] = Normalize(command.IssueDate),
            [IntakeVerifiedDataKeys.ExpiryDate] = Normalize(command.ExpiryDate),
            [IntakeVerifiedDataKeys.OfficialLetterDate] = Normalize(command.OfficialLetterDate),
            [IntakeVerifiedDataKeys.NewExpiryDate] = Normalize(command.NewExpiryDate),
            [IntakeVerifiedDataKeys.BankReference] = Normalize(command.BankReference),
            [IntakeVerifiedDataKeys.StatusStatement] = Normalize(command.StatusStatement),
            [IntakeVerifiedDataKeys.AttachmentNote] = Normalize(command.AttachmentNote),
            [IntakeVerifiedDataKeys.CaptureChannel] = command.CaptureChannel.ToString(),
            [IntakeVerifiedDataKeys.SourceSystemName] = Normalize(command.SourceSystemName),
            [IntakeVerifiedDataKeys.SourceReference] = Normalize(command.SourceReference),
            [IntakeVerifiedDataKeys.ExtractionRoute] = Normalize(command.ExtractionRouteResourceKey)
        };

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    private static OperationsReviewItem CreateOperationsReviewItem(
        Guarantee guarantee,
        GuaranteeDocument document,
        Guid? correspondenceId,
        IntakeScenarioDefinition scenario)
    {
        return new OperationsReviewItem(
            guarantee.Id,
            guarantee.GuaranteeNumber,
            document.Id,
            correspondenceId,
            scenario.Key,
            scenario.ReviewCategory,
            DateTimeOffset.UtcNow);
    }

    private static bool TryParseAmount(string? value, out decimal amount)
    {
        return StructuredInputParser.TryParseAmount(value, out amount);
    }

    private static bool TryParseDate(string? value, out DateOnly date)
    {
        return StructuredInputParser.TryParseDate(value, out date);
    }

    private static string? Normalize(string? value)
    {
        return StructuredInputParser.Normalize(value);
    }
}
