using System.Globalization;
using BG.Application.Contracts.Services;
using BG.Application.Models.Approvals;
using BG.Application.Models.Dispatch;
using BG.Application.ReferenceData;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BG.Infrastructure.Documents;

internal sealed class QuestPdfLetterGenerationService : ILetterGenerationService
{
    public Task<byte[]> GenerateLetterPdfAsync(
        DispatchLetterPreviewDto letter,
        IReadOnlyList<ApprovalPriorSignatureDto> signatures,
        CancellationToken cancellationToken = default)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var content = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2f, Unit.Centimetre);
                page.DefaultTextStyle(style => style.FontSize(11).FontFamily("Arial").DirectionFromRightToLeft());
                page.Content().Column(column =>
                {
                    column.Spacing(10);
                    column.Item().Text("KFSHRC - Bank Guarantee Dispatch").Bold().FontSize(14);
                    column.Item().Text($"Reference: {letter.ReferenceNumber}");
                    column.Item().Text($"Letter Date: {letter.LetterDate:yyyy-MM-dd}");
                    column.Item().Text($"Guarantee Number: {letter.GuaranteeNumber}");
                    column.Item().Text($"Bank: {letter.BankName}");
                    column.Item().Text($"Beneficiary: {letter.BeneficiaryName}");
                    column.Item().Text($"Principal: {letter.PrincipalName}");

                    column.Item().LineHorizontal(1);
                    column.Item().Text(GetLetterBody(letter));

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Cell().Element(CellStyle).Text("Current Amount").Bold();
                        table.Cell().Element(CellStyle).Text(FormatAmount(letter.CurrentAmount, letter.CurrencyCode));
                        table.Cell().Element(CellStyle).Text("Current Expiry").Bold();
                        table.Cell().Element(CellStyle).Text(letter.CurrentExpiryDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

                        if (letter.RequestedAmount.HasValue)
                        {
                            table.Cell().Element(CellStyle).Text("Requested Amount").Bold();
                            table.Cell().Element(CellStyle).Text(FormatAmount(letter.RequestedAmount.Value, letter.CurrencyCode));
                        }

                        if (letter.RequestedExpiryDate.HasValue)
                        {
                            table.Cell().Element(CellStyle).Text("Requested Expiry").Bold();
                            table.Cell().Element(CellStyle).Text(letter.RequestedExpiryDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                        }
                    });

                    if (signatures.Count > 0)
                    {
                        column.Item().PaddingTop(6).Text("Approval Signatures").Bold();

                        foreach (var signature in signatures.OrderBy(item => item.Sequence))
                        {
                            var signerName = string.IsNullOrWhiteSpace(signature.ResponsibleSignerDisplayName)
                                ? signature.ActorDisplayName
                                : signature.ResponsibleSignerDisplayName;
                            var stageLabel = signature.StageTitle ?? signature.StageTitleResourceKey ?? $"Stage {signature.Sequence}";

                            column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Column(signatureColumn =>
                            {
                                signatureColumn.Item().Text($"Stage: {stageLabel}");
                                signatureColumn.Item().Text($"Signer: {signerName}");
                                signatureColumn.Item().Text($"Role: {signature.StageRoleName}");
                                signatureColumn.Item().Text($"Acted At: {signature.ActedAtUtc.ToLocalTime():yyyy-MM-dd HH:mm}");
                            });
                        }
                    }
                });
            });
        }).GeneratePdf();

        return Task.FromResult(content);

        static IContainer CellStyle(IContainer container)
        {
            return container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4);
        }
    }

    private static string GetLetterBody(DispatchLetterPreviewDto letter)
    {
        return letter.RequestType switch
        {
            BG.Domain.Guarantees.GuaranteeRequestType.Extend when letter.RequestedExpiryDate.HasValue
                => $"Please extend guarantee {letter.GuaranteeNumber} until {letter.RequestedExpiryDate:yyyy-MM-dd}.",
            BG.Domain.Guarantees.GuaranteeRequestType.Reduce when letter.RequestedAmount.HasValue
                => $"Please reduce guarantee {letter.GuaranteeNumber} to {FormatAmount(letter.RequestedAmount.Value, letter.CurrencyCode)}.",
            BG.Domain.Guarantees.GuaranteeRequestType.Release
                => $"Please release guarantee {letter.GuaranteeNumber}.",
            BG.Domain.Guarantees.GuaranteeRequestType.ReplaceWithReducedGuarantee when letter.RequestedAmount.HasValue
                => $"Please replace guarantee {letter.GuaranteeNumber} with reduced amount {FormatAmount(letter.RequestedAmount.Value, letter.CurrencyCode)}.",
            BG.Domain.Guarantees.GuaranteeRequestType.VerifyStatus
                => $"Please confirm the current status of guarantee {letter.GuaranteeNumber}.",
            _ => $"Please process guarantee {letter.GuaranteeNumber} per approved request."
        };
    }

    private static string FormatAmount(decimal amount, string currencyCode)
    {
        return string.Format(CultureInfo.InvariantCulture, "{0:N2} {1}", amount, currencyCode);
    }
}
