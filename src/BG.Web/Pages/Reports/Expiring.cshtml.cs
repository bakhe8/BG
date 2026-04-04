using System.Globalization;
using System.Text;
using BG.Application.Contracts.Services.Reports;
using BG.Application.Models.Reports;
using BG.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BG.Web.Pages.Reports;

[Authorize(Policy = PermissionPolicyNames.ReportsView)]
public sealed class ExpiringModel : PageModel
{
    private readonly IReportService _reportService;

    public ExpiringModel(IReportService reportService)
    {
        _reportService = reportService;
    }

    [BindProperty(SupportsGet = true)]
    public int WithinDays { get; set; } = 30;

    public IReadOnlyList<ExpiringGuaranteeItemDto> Items { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Items = await _reportService.GetExpiringAsync(WithinDays, cancellationToken);
    }

    public async Task<IActionResult> OnGetCsvAsync(CancellationToken cancellationToken)
    {
        var items = await _reportService.GetExpiringAsync(WithinDays, cancellationToken);

        var csv = new StringBuilder();
        csv.AppendLine("GuaranteeNumber,BankName,BeneficiaryName,Amount,Currency,ExpiryDate,DaysRemaining");

        foreach (var item in items)
        {
            csv.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "{0},{1},{2},{3},{4},{5},{6}",
                EscapeCsv(item.GuaranteeNumber),
                EscapeCsv(item.BankName),
                EscapeCsv(item.BeneficiaryName),
                item.CurrentAmount,
                item.CurrencyCode,
                item.ExpiryDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                item.DaysUntilExpiry));
        }

        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "expiring-guarantees.csv");
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
