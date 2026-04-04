using BG.Application.Contracts.Persistence;
using BG.Application.ReferenceData;
using BG.Application.Requests;
using BG.Application.Services;
using BG.Domain.Guarantees;
using BG.Domain.Workflow;

namespace BG.UnitTests.Application;

public sealed class GuaranteeHistoryServiceTests
{
    [Fact]
    public async Task GetGuaranteeHistoryAsync_returns_events_for_owner_with_requests_view_permission()
    {
        var ownerId = Guid.NewGuid();
        var guarantee = CreateGuaranteeWithEvents(ownerId);
        var service = new GuaranteeHistoryService(new StubGuaranteeHistoryRepository(guarantee));

        var result = await service.GetGuaranteeHistoryAsync(
            guarantee.GuaranteeNumber,
            ownerId,
            ["requests.view"],
            pageNumber: 1,
            pageSize: 10);

        Assert.True(result.Succeeded, result.ErrorCode);
        Assert.NotEmpty(result.Value!.Items);
        Assert.Equal("GuaranteeEvent_RequestSubmittedForApproval", result.Value.Items[0].EventTypeResourceKey);
    }

    [Fact]
    public async Task GetGuaranteeHistoryAsync_rejects_requests_only_user_without_ownership()
    {
        var ownerId = Guid.NewGuid();
        var guarantee = CreateGuaranteeWithEvents(ownerId);
        var service = new GuaranteeHistoryService(new StubGuaranteeHistoryRepository(guarantee));

        var result = await service.GetGuaranteeHistoryAsync(
            guarantee.GuaranteeNumber,
            Guid.NewGuid(),
            ["requests.view"],
            pageNumber: 1,
            pageSize: 10);

        Assert.False(result.Succeeded);
        Assert.Equal(RequestErrorCodes.UserContextInvalid, result.ErrorCode);
    }

    [Fact]
    public async Task GetGuaranteeHistoryAsync_allows_operational_permission_without_ownership()
    {
        var ownerId = Guid.NewGuid();
        var guarantee = CreateGuaranteeWithEvents(ownerId);
        var service = new GuaranteeHistoryService(new StubGuaranteeHistoryRepository(guarantee));

        var result = await service.GetGuaranteeHistoryAsync(
            guarantee.GuaranteeNumber,
            Guid.NewGuid(),
            ["operations.queue.view"],
            pageNumber: 1,
            pageSize: 10);

        Assert.True(result.Succeeded, result.ErrorCode);
        Assert.NotEmpty(result.Value!.Items);
    }

    [Fact]
    public async Task GetGuaranteeHistoryAsync_orders_events_descending_and_applies_paging()
    {
        var ownerId = Guid.NewGuid();
        var guarantee = CreateGuaranteeWithEvents(ownerId);
        var service = new GuaranteeHistoryService(new StubGuaranteeHistoryRepository(guarantee));

        var result = await service.GetGuaranteeHistoryAsync(
            guarantee.GuaranteeNumber,
            ownerId,
            ["requests.view"],
            pageNumber: 1,
            pageSize: 1);

        Assert.True(result.Succeeded, result.ErrorCode);
        Assert.Single(result.Value!.Items);
        Assert.Equal(3, result.Value.PageInfo.TotalItemCount);
        Assert.Equal(3, result.Value.PageInfo.TotalPageCount);
        Assert.Equal(GuaranteeEventResourceCatalog.GetResourceKey(GuaranteeEventType.RequestSubmittedForApproval), result.Value.Items[0].EventTypeResourceKey);
    }

    [Fact]
    public void GuaranteeEventResourceCatalog_maps_all_event_types()
    {
        foreach (var eventType in Enum.GetValues<GuaranteeEventType>())
        {
            var resourceKey = GuaranteeEventResourceCatalog.GetResourceKey(eventType);
            var iconKey = GuaranteeEventResourceCatalog.GetIconKey(eventType);

            Assert.False(string.IsNullOrWhiteSpace(resourceKey));
            Assert.False(string.IsNullOrWhiteSpace(iconKey));
        }
    }

    private static Guarantee CreateGuaranteeWithEvents(Guid ownerId)
    {
        var guarantee = Guarantee.RegisterNew(
            "BG-2026-9001",
            "Saudi National Bank",
            "KFSHRC",
            "Prime Vendor",
            GuaranteeCategory.Contract,
            100000m,
            "SAR",
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            DateTimeOffset.UtcNow.AddDays(-10));

        var request = guarantee.CreateRequest(
            ownerId,
            GuaranteeRequestType.Extend,
            null,
            new DateOnly(2027, 6, 30),
            "Need extension",
            DateTimeOffset.UtcNow.AddDays(-5),
            "Request Owner");

        var process = new RequestApprovalProcess(request.Id, Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(-4));
        process.AddStage(
            Guid.NewGuid(),
            "WorkflowStage_GuaranteesSupervisor_Title",
            "WorkflowStage_GuaranteesSupervisor_Summary",
            "Guarantees Supervisor",
            "Stage summary",
            true);
        process.Start();

        request.SubmitForApproval(process);
        guarantee.RecordRequestSubmittedForApproval(
            request.Id,
            DateTimeOffset.UtcNow.AddDays(-3),
            ownerId,
            "Request Owner",
            "Guarantees Supervisor");

        return guarantee;
    }

    private sealed class StubGuaranteeHistoryRepository : IGuaranteeHistoryRepository
    {
        private readonly Guarantee _guarantee;

        public StubGuaranteeHistoryRepository(Guarantee guarantee)
        {
            _guarantee = guarantee;
        }

        public Task<Guarantee?> GetGuaranteeWithEventsAsync(string guaranteeNumber, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Guarantee?>(
                string.Equals(_guarantee.GuaranteeNumber, guaranteeNumber, StringComparison.OrdinalIgnoreCase)
                    ? _guarantee
                    : null);
        }
    }
}
