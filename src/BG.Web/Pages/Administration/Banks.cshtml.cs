using BG.Application.Contracts.Services;
using BG.Application.Models.Administration;
using BG.Domain.Guarantees;
using BG.Web.Localization;
using BG.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace BG.Web.Pages.Administration;

[Authorize(Policy = PermissionPolicyNames.BanksManage)]
public sealed class BanksModel : PageModel
{
    private readonly IBankAdministrationService _bankAdministrationService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public BanksModel(
        IBankAdministrationService bankAdministrationService,
        IStringLocalizer<SharedResource> localizer)
    {
        _bankAdministrationService = bankAdministrationService;
        _localizer = localizer;
    }

    [FromQuery(Name = "bank")]
    public Guid? SelectedBankId { get; set; }

    [BindProperty]
    public CreateBankInput CreateInput { get; set; } = new();

    [BindProperty]
    public UpdateBankInput UpdateInput { get; set; } = new();

    public IReadOnlyList<BankSummaryDto> Banks { get; private set; } = Array.Empty<BankSummaryDto>();

    public BankSummaryDto? ActiveBank { get; private set; }

    public IReadOnlyList<GuaranteeDispatchChannel> DispatchChannelOptions { get; } = Enum.GetValues<GuaranteeDispatchChannel>();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostCreateAsync(CancellationToken cancellationToken)
    {
        var result = await _bankAdministrationService.CreateBankAsync(
            new CreateBankCommand(
                CreateInput.CanonicalName,
                CreateInput.ShortCode,
                CreateInput.OfficialEmail,
                CreateInput.IsEmailDispatchEnabled,
                CreateInput.SupportedDispatchChannels,
                CreateInput.Notes),
            cancellationToken);

        if (result.Succeeded)
        {
            StatusMessage = _localizer["Administration_Banks_CreateSuccess"];
            return RedirectToPage(new { bank = result.Value!.Id });
        }

        ModelState.AddModelError(string.Empty, _localizer[result.ErrorCode!]);
        await LoadAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateAsync(CancellationToken cancellationToken)
    {
        var result = await _bankAdministrationService.UpdateBankAsync(
            new UpdateBankCommand(
                UpdateInput.BankId,
                UpdateInput.CanonicalName,
                UpdateInput.ShortCode,
                UpdateInput.OfficialEmail,
                UpdateInput.IsEmailDispatchEnabled,
                UpdateInput.SupportedDispatchChannels,
                UpdateInput.Notes),
            cancellationToken);

        if (result.Succeeded)
        {
            StatusMessage = _localizer["Administration_Banks_UpdateSuccess"];
            return RedirectToPage(new { bank = result.Value!.Id });
        }

        ModelState.AddModelError(string.Empty, _localizer[result.ErrorCode!]);
        SelectedBankId = UpdateInput.BankId;
        await LoadAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostDeactivateAsync(Guid bankId, CancellationToken cancellationToken)
    {
        var result = await _bankAdministrationService.DeactivateBankAsync(bankId, cancellationToken);

        if (result.Succeeded)
        {
            StatusMessage = _localizer["Administration_Banks_DeactivateSuccess"];
            return RedirectToPage(new { bank = bankId });
        }

        ModelState.AddModelError(string.Empty, _localizer[result.ErrorCode!]);
        SelectedBankId = bankId;
        await LoadAsync(cancellationToken);
        return Page();
    }

    public IDictionary<string, string> BuildSelectionRoute(Guid bankId)
    {
        return new Dictionary<string, string>
        {
            ["bank"] = bankId.ToString()
        };
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        Banks = await _bankAdministrationService.GetBanksAsync(cancellationToken);
        ActiveBank = ResolveActiveBank(SelectedBankId);
        SelectedBankId = ActiveBank?.Id;

        if (ActiveBank is not null)
        {
            UpdateInput = new UpdateBankInput
            {
                BankId = ActiveBank.Id,
                CanonicalName = ActiveBank.CanonicalName,
                ShortCode = ActiveBank.ShortCode,
                OfficialEmail = ActiveBank.OfficialEmail,
                IsEmailDispatchEnabled = ActiveBank.IsEmailDispatchEnabled,
                SupportedDispatchChannels = ActiveBank.SupportedDispatchChannels.ToList(),
                Notes = ActiveBank.Notes
            };
        }
    }

    private BankSummaryDto? ResolveActiveBank(Guid? selectedBankId)
    {
        if (Banks.Count == 0)
        {
            return null;
        }

        if (selectedBankId.HasValue)
        {
            var selected = Banks.FirstOrDefault(bank => bank.Id == selectedBankId.Value);
            if (selected is not null)
            {
                return selected;
            }
        }

        return Banks[0];
    }

    public sealed class CreateBankInput
    {
        public string CanonicalName { get; set; } = string.Empty;

        public string ShortCode { get; set; } = string.Empty;

        public string? OfficialEmail { get; set; }

        public bool IsEmailDispatchEnabled { get; set; }

        public List<GuaranteeDispatchChannel> SupportedDispatchChannels { get; set; } = [GuaranteeDispatchChannel.Courier];

        public string? Notes { get; set; }
    }

    public sealed class UpdateBankInput
    {
        public Guid BankId { get; set; }

        public string CanonicalName { get; set; } = string.Empty;

        public string ShortCode { get; set; } = string.Empty;

        public string? OfficialEmail { get; set; }

        public bool IsEmailDispatchEnabled { get; set; }

        public List<GuaranteeDispatchChannel> SupportedDispatchChannels { get; set; } = [GuaranteeDispatchChannel.Courier];

        public string? Notes { get; set; }
    }
}
