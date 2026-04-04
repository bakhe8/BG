using System.Globalization;
using System.Text;
using BG.Application.Contracts.Services.Reports;
using BG.Application.Models.Reports;
using BG.Domain.Guarantees;
using BG.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BG.Web.Pages.Reports;

[Authorize(Policy = PermissionPolicyNames.ReportsView)]
public sealed class ActivityModel : PageModel
{
    private readonly IReportService _reportService;

    public ActivityModel(IReportService reportService)
    {
        _reportService = reportService;
    }

    [BindProperty(SupportsGet = true)]
    public GuaranteeRequestStatus? FilterStatus { get; set; }

    [BindProperty(SupportsGet = true)]
    public GuaranteeRequestType? FilterType { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTimeOffset? FilterFrom { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTimeOffset? FilterTo { get; set; }

    public IReadOnlyList<RequestActivityItemDto> Items { get; private set; } = [];

    public IReadOnlyList<GuaranteeRequestStatus> StatusOptions { get; } = Enum.GetValues<GuaranteeRequestStatus>();

    public IReadOnlyList<GuaranteeRequestType> TypeOptions { get; } = Enum.GetValues<GuaranteeRequestType>();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Items = await _reportService.GetRequestActivityAsync(
            FilterStatus, FilterType, FilterFrom, FilterTo, cancellationToken);
    }

    public async Task<IActionResult> OnGetCsvAsync(CancellationToken cancellationToken)
    {
        var items = await _reportService.GetRequestActivityAsync(
            FilterStatus, FilterType, FilterFrom, FilterTo, cancellationToken);

        var csv = new StringBuilder();
        csv.AppendLine("RequestId,GuaranteeNumber,RequestType,Status,RequestedBy,CreatedAt,CompletedAt");

        foreach (var item in items)
        {
            csv.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "{0},{1},{2},{3},{4},{5},{6}",
                item.RequestId,
                EscapeCsv(item.GuaranteeNumber),
                item.RequestType,
                item.Status,
                EscapeCsv(item.RequestedByDisplayName),
                item.CreatedAtUtc.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                item.CompletedAtUtc?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) ?? string.Empty));
        }

        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "request-activity.csv");
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
