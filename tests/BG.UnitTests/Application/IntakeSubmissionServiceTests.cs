using BG.Application.Contracts.Persistence;
using BG.Application.Contracts.Services;
using BG.Application.Intake;
using BG.Application.Models.Intake;
using BG.Application.ReferenceData;
using BG.Application.Services;
using BG.Domain.Guarantees;
using BG.Domain.Identity;
using BG.Domain.Notifications;
using BG.Domain.Operations;

namespace BG.UnitTests.Application;

public sealed class IntakeSubmissionServiceTests
{
    [Fact]
    public async Task BeginExtractionAsync_stages_file_and_returns_draft()
    {
        var service = CreateService(
            documentStore: new StubDocumentStore(),
            extractionEngine: new LocalIntakeExtractionEngine());

        await using var stream = new MemoryStream("scan"u8.ToArray());
        var result = await service.BeginExtractionAsync(
            new BeginIntakeExtractionCommand("new-guarantee", "BG-2026-0451.pdf", stream));

        Assert.True(result.Succeeded);
        Assert.Equal("new-guarantee", result.Value!.ScenarioKey);
        Assert.Equal("token-1", result.Value.StagedDocumentToken);
        Assert.Equal("IntakeExtractionRoute_PdfTextFirst", result.Value.ExtractionRouteResourceKey);
        Assert.Contains(result.Value.Fields, field => field.FieldKey == "IntakeField_GuaranteeNumber" && field.Value == "BG-2026-0451");
    }

    [Fact]
    public async Task FinalizeAsync_for_new_guarantee_creates_guarantee_document_with_capture_provenance()
    {
        var actor = CreateActor();
        var repository = new StubIntakeRepository(actor);
        var service = CreateService(repository);

        var result = await service.FinalizeAsync(
            new IntakeSubmissionCommand(
                actor.Id,
                "new-guarantee",
                "token-1",
                GuaranteeDocumentFormKeys.GuaranteeInstrumentSnb,
                "IntakeExtractionRoute_PdfTextFirst",
                2,
                GuaranteeDocumentCaptureChannel.ScanStation,
                "Fujitsu fi-8170",
                "batch-2026-03-12-01",
                "BG-2026-0201",
                "Saudi National Bank",
                "KFSHRC",
                "Main Contractor",
                GuaranteeCategory.Contract,
                "1250000",
                "SAR",
                "2026-03-11",
                "2027-03-11",
                null,
                null,
                null,
                null,
                null));

        Assert.True(result.Succeeded);
        Assert.True(repository.SaveChangesCalled);

        var guarantee = Assert.Single(repository.AddedGuarantees);
        var document = Assert.Single(guarantee.Documents);
        Assert.Equal(GuaranteeDocumentType.GuaranteeInstrument, document.DocumentType);
        Assert.Equal(GuaranteeDocumentSourceType.Scanned, document.SourceType);
        Assert.Equal(actor.Id, document.CapturedByUserId);
        Assert.Equal(actor.DisplayName, document.CapturedByDisplayName);
        Assert.Equal(GuaranteeDocumentCaptureChannel.ScanStation, document.CaptureChannel);
        Assert.Equal("Fujitsu fi-8170", document.SourceSystemName);
        Assert.Equal("batch-2026-03-12-01", document.SourceReference);
        Assert.Contains("\"documentFormKey\":\"guarantee-instrument-snb\"", document.VerifiedDataJson);
        Assert.Contains("\"captureChannel\":\"ScanStation\"", document.VerifiedDataJson);
        Assert.Contains("\"sourceSystemName\":\"Fujitsu fi-8170\"", document.VerifiedDataJson);

        var ledgerEntry = Assert.Single(guarantee.Events.Where(guaranteeEvent => guaranteeEvent.EventType == GuaranteeEventType.DocumentCaptured));
        Assert.Equal(actor.DisplayName, ledgerEntry.ActorDisplayName);
        Assert.Equal(document.Id, ledgerEntry.GuaranteeDocumentId);
        Assert.Contains("ScanStation", ledgerEntry.Summary);
        Assert.Contains("Fujitsu fi-8170", ledgerEntry.Summary);

        var reviewItem = Assert.Single(repository.AddedReviewItems);
        Assert.Equal("BG-2026-0201", reviewItem.GuaranteeNumber);
        Assert.Equal(OperationsReviewItemCategory.GuaranteeRegistration, reviewItem.Category);
        Assert.Equal(OperationsReviewItemStatus.Pending, reviewItem.Status);
    }

    [Fact]
    public async Task FinalizeAsync_for_extension_confirmation_adds_correspondence_without_mutating_guarantee()
    {
        var actor = CreateActor();
        var guarantee = Guarantee.RegisterNew(
            "BG-2026-0301",
            "National Bank",
            "KFSHRC",
            "Main Contractor",
            GuaranteeCategory.Contract,
            500000m,
            "SAR",
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            DateTimeOffset.UtcNow);

        var repository = new StubIntakeRepository(actor, guarantee);
        var service = CreateService(repository);

        var result = await service.FinalizeAsync(
            new IntakeSubmissionCommand(
                actor.Id,
                "extension-confirmation",
                "token-1",
                GuaranteeDocumentFormKeys.BankLetterGeneric,
                "IntakeExtractionRoute_PdfTextFirst",
                1,
                GuaranteeDocumentCaptureChannel.ManualUpload,
                null,
                null,
                "BG-2026-0301",
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                "2026-03-20",
                "2027-06-30",
                "EXT-2001",
                null,
                null));

        Assert.True(result.Succeeded);
        Assert.Equal(new DateOnly(2026, 12, 31), guarantee.ExpiryDate);

        var document = Assert.Single(guarantee.Documents);
        Assert.Equal(GuaranteeDocumentType.BankResponse, document.DocumentType);
        Assert.Equal(GuaranteeDocumentSourceType.Uploaded, document.SourceType);
        Assert.Equal(actor.DisplayName, document.CapturedByDisplayName);

        var correspondence = Assert.Single(guarantee.Correspondence);
        Assert.Equal(GuaranteeCorrespondenceDirection.Incoming, correspondence.Direction);
        Assert.Equal(GuaranteeCorrespondenceKind.BankConfirmation, correspondence.Kind);
        Assert.Equal("EXT-2001", correspondence.ReferenceNumber);

        var reviewItem = Assert.Single(repository.AddedReviewItems);
        Assert.Equal(OperationsReviewItemCategory.IncomingBankConfirmation, reviewItem.Category);
        Assert.Equal(correspondence.Id, reviewItem.GuaranteeCorrespondenceId);
    }

    [Fact]
    public async Task FinalizeAsync_for_reduction_confirmation_stores_confirmed_amount_without_mutating_guarantee()
    {
        var actor = CreateActor();
        var guarantee = Guarantee.RegisterNew(
            "BG-2026-0302",
            "National Bank",
            "KFSHRC",
            "Main Contractor",
            GuaranteeCategory.Contract,
            500000m,
            "SAR",
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            DateTimeOffset.UtcNow);

        var repository = new StubIntakeRepository(actor, guarantee);
        var service = CreateService(repository);

        var result = await service.FinalizeAsync(
            new IntakeSubmissionCommand(
                actor.Id,
                "reduction-confirmation",
                "token-1",
                GuaranteeDocumentFormKeys.BankLetterGeneric,
                "IntakeExtractionRoute_PdfTextFirst",
                1,
                GuaranteeDocumentCaptureChannel.OracleImport,
                "Oracle ECM",
                "doc-7781",
                "BG-2026-0302",
                null,
                null,
                null,
                null,
                "420000",
                null,
                null,
                null,
                "2026-03-22",
                null,
                "RED-2001",
                null,
                null));

        Assert.True(result.Succeeded);
        Assert.Equal(500000m, guarantee.CurrentAmount);

        var document = Assert.Single(guarantee.Documents);
        Assert.Equal(GuaranteeDocumentSourceType.Imported, document.SourceType);
        Assert.Equal("Oracle ECM", document.SourceSystemName);
        Assert.Equal("doc-7781", document.SourceReference);
        Assert.Contains("\"amount\":\"420000\"", document.VerifiedDataJson);
        Assert.Contains("Confirmed amount: 420000.", document.Notes);

        var correspondence = Assert.Single(guarantee.Correspondence);
        Assert.Equal(GuaranteeCorrespondenceKind.BankConfirmation, correspondence.Kind);
        Assert.Equal("RED-2001", correspondence.ReferenceNumber);

        var reviewItem = Assert.Single(repository.AddedReviewItems);
        Assert.Equal(OperationsReviewItemCategory.IncomingBankConfirmation, reviewItem.Category);
        Assert.Equal(correspondence.Id, reviewItem.GuaranteeCorrespondenceId);
    }

    [Fact]
    public async Task FinalizeAsync_requires_source_reference_for_oracle_import()
    {
        var actor = CreateActor();
        var service = CreateService(new StubIntakeRepository(actor));

        var result = await service.FinalizeAsync(
            new IntakeSubmissionCommand(
                actor.Id,
                "new-guarantee",
                "token-1",
                GuaranteeDocumentFormKeys.GuaranteeInstrumentGeneric,
                "IntakeExtractionRoute_PdfTextFirst",
                1,
                GuaranteeDocumentCaptureChannel.OracleImport,
                "Oracle ECM",
                null,
                "BG-2026-9001",
                "Saudi National Bank",
                "KFSHRC",
                "Main Contractor",
                GuaranteeCategory.Contract,
                "150000",
                "SAR",
                "2026-03-11",
                "2027-03-11",
                null,
                null,
                null,
                null,
                null));

        Assert.False(result.Succeeded);
        Assert.Equal(IntakeErrorCodes.SourceReferenceRequired, result.ErrorCode);
    }

    [Fact]
    public async Task FinalizeAsync_rejects_document_form_that_is_not_supported_for_scenario()
    {
        var actor = CreateActor();
        var service = CreateService(new StubIntakeRepository(actor));

        var result = await service.FinalizeAsync(
            new IntakeSubmissionCommand(
                actor.Id,
                "new-guarantee",
                "token-1",
                GuaranteeDocumentFormKeys.BankLetterSnb,
                "IntakeExtractionRoute_PdfTextFirst",
                1,
                GuaranteeDocumentCaptureChannel.ManualUpload,
                null,
                null,
                "BG-2026-9002",
                "Saudi National Bank",
                "KFSHRC",
                "Main Contractor",
                GuaranteeCategory.Contract,
                "150000",
                "SAR",
                "2026-03-11",
                "2027-03-11",
                null,
                null,
                null,
                null,
                null));

        Assert.False(result.Succeeded);
        Assert.Equal(IntakeErrorCodes.DocumentFormInvalid, result.ErrorCode);
    }

    private static IntakeSubmissionService CreateService(
        StubIntakeRepository? repository = null,
        IIntakeDocumentStore? documentStore = null,
        IIntakeExtractionEngine? extractionEngine = null)
    {
        return new IntakeSubmissionService(
            repository ?? new StubIntakeRepository(CreateActor()),
            documentStore ?? new StubDocumentStore(),
            extractionEngine ?? new LocalIntakeExtractionEngine(),
            new StubNotificationService());
    }

    private static User CreateActor()
    {
        var role = new Role("Intake Specialist", "Intake role");
        role.AssignPermissions(
        [
            new Permission("intake.view", "Intake"),
            new Permission("intake.verify", "Intake"),
            new Permission("intake.finalize", "Intake")
        ]);

        var actor = new User(
            "intake.specialist",
            "Intake Specialist",
            "intake.specialist@bg.local",
            externalId: null,
            UserSourceType.Local,
            isActive: true,
            createdAtUtc: DateTimeOffset.UtcNow);
        actor.AssignRoles([role]);
        return actor;
    }

    private sealed class StubIntakeRepository : IIntakeRepository
    {
        private readonly Dictionary<string, Guarantee> _guarantees;
        private readonly IReadOnlyList<User> _actors;

        public StubIntakeRepository(User actor, params Guarantee[] guarantees)
        {
            _actors = [actor];
            _guarantees = guarantees.ToDictionary(guarantee => guarantee.GuaranteeNumber, StringComparer.Ordinal);
        }

        public List<Guarantee> AddedGuarantees { get; } = [];

        public List<OperationsReviewItem> AddedReviewItems { get; } = [];

        public bool SaveChangesCalled { get; private set; }

        public Task<IReadOnlyList<User>> ListIntakeActorsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_actors);
        }

        public Task<User?> GetIntakeActorByIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_actors.SingleOrDefault(actor => actor.Id == userId));
        }

        public Task<bool> GuaranteeNumberExistsAsync(string guaranteeNumber, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_guarantees.ContainsKey(guaranteeNumber));
        }

        public Task<Guarantee?> GetGuaranteeByNumberAsync(string guaranteeNumber, CancellationToken cancellationToken = default)
        {
            _guarantees.TryGetValue(guaranteeNumber, out var guarantee);
            return Task.FromResult(guarantee);
        }

        public Task AddGuaranteeAsync(Guarantee guarantee, CancellationToken cancellationToken = default)
        {
            AddedGuarantees.Add(guarantee);
            _guarantees[guarantee.GuaranteeNumber] = guarantee;
            return Task.CompletedTask;
        }

        public Task AddOperationsReviewItemAsync(OperationsReviewItem reviewItem, CancellationToken cancellationToken = default)
        {
            AddedReviewItems.Add(reviewItem);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class StubDocumentStore : IIntakeDocumentStore
    {
        public Task<StagedIntakeDocumentDto> StageAsync(string originalFileName, Stream content, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new StagedIntakeDocumentDto("token-1", originalFileName, 256));
        }

        public Task<StagedIntakeDocumentDto?> GetStagedAsync(string stagedDocumentToken, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<StagedIntakeDocumentDto?>(
                string.Equals(stagedDocumentToken, "token-1", StringComparison.Ordinal)
                    ? new StagedIntakeDocumentDto("token-1", "scan.pdf", 256, "C:\\staging\\scan.pdf")
                    : null);
        }

        public Task<PromotedIntakeDocumentDto> PromoteAsync(string stagedDocumentToken, string guaranteeNumber, CancellationToken cancellationToken = default)
        {
            if (!string.Equals(stagedDocumentToken, "token-1", StringComparison.Ordinal))
            {
                throw new FileNotFoundException();
            }

            return Task.FromResult(
                new PromotedIntakeDocumentDto(
                    "scan.pdf",
                    $"guarantees/{guaranteeNumber}/scan.pdf",
                    256));
        }

        public Stream GetDocumentContent(string storagePath)
        {
            if (File.Exists(storagePath))
            {
                return File.OpenRead(storagePath);
            }

            return new MemoryStream();
        }
    }

    private sealed class StubNotificationService : INotificationService
    {
        public Task SendNotificationAsync(string message, string? link, string requiredPermission, Guid? targetUserId = null, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<IEnumerable<Notification>> GetUserNotificationsAsync(Guid userId, string[] userPermissions, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IEnumerable<Notification>>([]);
        }

        public Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
