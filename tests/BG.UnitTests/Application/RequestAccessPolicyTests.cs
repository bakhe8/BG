using BG.Application.Requests;
using BG.Domain.Guarantees;

namespace BG.UnitTests.Application;

public sealed class RequestAccessPolicyTests
{
    private static readonly Guid RequestOwnerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OtherUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public void CanViewRequest_returns_true_only_for_request_owner()
    {
        var guarantee = CreateGuarantee();
        var request = guarantee.CreateRequest(
            RequestOwnerId,
            GuaranteeRequestType.Extend,
            requestedAmount: null,
            requestedExpiryDate: new DateOnly(2027, 6, 30),
            notes: "Owner request",
            createdAtUtc: DateTimeOffset.UtcNow);

        Assert.True(RequestAccessPolicy.CanViewRequest(RequestOwnerId, request));
        Assert.False(RequestAccessPolicy.CanViewRequest(OtherUserId, request));
    }

    [Fact]
    public void CanViewRequestEvent_returns_true_only_for_owner_and_matching_request_event()
    {
        var guarantee = CreateGuarantee();
        var request = guarantee.CreateRequest(
            RequestOwnerId,
            GuaranteeRequestType.VerifyStatus,
            requestedAmount: null,
            requestedExpiryDate: null,
            notes: "Owner request",
            createdAtUtc: DateTimeOffset.UtcNow);

        var requestEvent = Assert.Single(guarantee.Events.Where(item => item.GuaranteeRequestId == request.Id));

        Assert.True(RequestAccessPolicy.CanViewRequestEvent(RequestOwnerId, request, requestEvent));
        Assert.False(RequestAccessPolicy.CanViewRequestEvent(OtherUserId, request, requestEvent));
    }

    private static Guarantee CreateGuarantee()
    {
        return Guarantee.RegisterNew(
            "BG-2026-REQ-0001",
            "National Bank",
            "KFSHRC",
            "Main Contractor",
            GuaranteeCategory.Contract,
            100000m,
            "SAR",
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            DateTimeOffset.UtcNow);
    }
}
