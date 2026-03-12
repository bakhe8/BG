using BG.Application.Common;
using BG.Application.Contracts.Services;
using BG.Application.Models.Approvals;
using BG.Web.Localization;
using BG.Web.Pages.Administration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace BG.UnitTests.Web;

public sealed class ApprovalDelegationsPageTests
{
    [Fact]
    public async Task OnGetAsync_loads_snapshot()
    {
        var model = new DelegationsModel(new StubApprovalDelegationAdministrationService(), new PassThroughLocalizer());

        await model.OnGetAsync(CancellationToken.None);

        Assert.Single(model.Snapshot.AvailableUsers);
        Assert.Single(model.Snapshot.AvailableRoles);
    }

    [Fact]
    public async Task OnPostCreateAsync_redirects_on_success()
    {
        var service = new StubApprovalDelegationAdministrationService();
        var model = new DelegationsModel(service, new PassThroughLocalizer())
        {
            Input = new DelegationsModel.CreateApprovalDelegationInput
            {
                DelegatorUserId = service.User.Id,
                DelegateUserId = Guid.NewGuid(),
                RoleId = service.Role.Id,
                StartsAtUtc = DateTimeOffset.UtcNow.AddHours(1),
                EndsAtUtc = DateTimeOffset.UtcNow.AddHours(8),
                Reason = "Vacation"
            }
        };

        var result = await model.OnPostCreateAsync(CancellationToken.None);

        Assert.IsType<RedirectToPageResult>(result);
        Assert.NotNull(model.StatusMessage);
    }

    private sealed class StubApprovalDelegationAdministrationService : IApprovalDelegationAdministrationService
    {
        public StubApprovalDelegationAdministrationService()
        {
            User = new ApprovalDelegationUserOptionDto(Guid.NewGuid(), "delegator.one", "Delegator One");
            Role = new ApprovalDelegationRoleOptionDto(Guid.NewGuid(), "Guarantees Supervisor", "Approves requests");
            Snapshot = new ApprovalDelegationAdministrationSnapshotDto([], [User], [Role]);
        }

        public ApprovalDelegationAdministrationSnapshotDto Snapshot { get; }

        public ApprovalDelegationUserOptionDto User { get; }

        public ApprovalDelegationRoleOptionDto Role { get; }

        public Task<ApprovalDelegationAdministrationSnapshotDto> GetSnapshotAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Snapshot);
        }

        public Task<OperationResult<Guid>> CreateAsync(CreateApprovalDelegationCommand command, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(OperationResult<Guid>.Success(Guid.NewGuid()));
        }

        public Task<OperationResult<Guid>> RevokeAsync(RevokeApprovalDelegationCommand command, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(OperationResult<Guid>.Success(command.DelegationId));
        }
    }

    private sealed class PassThroughLocalizer : IStringLocalizer<SharedResource>
    {
        public LocalizedString this[string name] => new(name, name);

        public LocalizedString this[string name, params object[] arguments] => new(name, string.Format(name, arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            return Array.Empty<LocalizedString>();
        }

        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture)
        {
            return this;
        }
    }
}
