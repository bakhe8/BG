using System.Globalization;
using System.Text;
using BG.Application.Contracts.Services.Reports;
using BG.Application.Models.Reports;
using BG.Domain.Guarantees;
using BG.Web.Localization;
using BG.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BG.Web.Pages.Reports;

[Authorize(Policy = PermissionPolicyNames.ReportsView)]
public sealed class PortfolioModel : PageModel
{
    private readonly IReportService _reportService;

    public PortfolioModel(IReportService reportService)
    {
        _reportService = reportService;
    }

    [BindProperty(SupportsGet = true)]
    public GuaranteeStatus? FilterStatus { get; set; }

    [BindProperty(SupportsGet = true)]
    public GuaranteeCategory? FilterCategory { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FilterBank { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateOnly? FilterFrom { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateOnly? FilterTo { get; set; }

    public IReadOnlyList<GuaranteePortfolioItemDto> Items { get; private set; } = [];

    public IReadOnlyList<GuaranteeStatus> StatusOptions { get; } = Enum.GetValues<GuaranteeStatus>();

    public IReadOnlyList<GuaranteeCategory> CategoryOptions { get; } = Enum.GetValues<GuaranteeCategory>();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Items = await _reportService.GetPortfolioAsync(
            FilterStatus, FilterCategory, FilterBank,
            FilterFrom, FilterTo, cancellationToken);
    }

    public async Task<IActionResult> OnGetCsvAsync(CancellationToken cancellationToken)
    {
        var items = await _reportService.GetPortfolioAsync(
            FilterStatus, FilterCategory, FilterBank,
            FilterFrom, FilterTo, cancellationToken);

        var csv = new StringBuilder();
        csv.AppendLine("GuaranteeNumber,BankName,BeneficiaryName,PrincipalName,Category,Status,Amount,Currency,IssueDate,ExpiryDate");

        foreach (var item in items)
        {
            csv.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}",
                EscapeCsv(item.GuaranteeNumber),
                EscapeCsv(item.BankName),
                EscapeCsv(item.BeneficiaryName),
                EscapeCsv(item.PrincipalName),
                item.Category,
                item.Status,
                item.CurrentAmount,
                item.CurrencyCode,
                item.IssueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                item.ExpiryDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
        }

        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "guarantee-portfolio.csv");
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
