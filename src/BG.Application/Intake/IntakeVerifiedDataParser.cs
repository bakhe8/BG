using System.Text.Json;
using BG.Application.ReferenceData;
using BG.Domain.Guarantees;

namespace BG.Application.Intake;

internal static class IntakeVerifiedDataParser
{
    public static IntakeVerifiedDataSnapshot Parse(string? verifiedDataJson)
    {
        if (string.IsNullOrWhiteSpace(verifiedDataJson))
        {
            return new IntakeVerifiedDataSnapshot(null, null, null, null, null, null, null);
        }

        try
        {
            using var document = JsonDocument.Parse(verifiedDataJson);
            var root = document.RootElement;

            return new IntakeVerifiedDataSnapshot(
                TryReadString(root, IntakeVerifiedDataKeys.ScenarioKey),
                TryReadString(root, IntakeVerifiedDataKeys.DocumentFormKey),
                TryReadString(root, IntakeVerifiedDataKeys.GuaranteeNumber),
                TryReadString(root, IntakeVerifiedDataKeys.BankName),
                TryReadString(root, IntakeVerifiedDataKeys.Amount),
                TryReadString(root, IntakeVerifiedDataKeys.NewExpiryDate),
                TryReadString(root, IntakeVerifiedDataKeys.StatusStatement));
        }
        catch (JsonException)
        {
            return new IntakeVerifiedDataSnapshot(null, null, null, null, null, null, null);
        }
    }

    public static GuaranteeDocumentFormDefinition? ResolveDocumentForm(
        GuaranteeDocumentType documentType,
        string? verifiedDataJson)
    {
        var snapshot = Parse(verifiedDataJson);

        if (!string.IsNullOrWhiteSpace(snapshot.DocumentFormKey))
        {
            var explicitForm = GuaranteeDocumentFormCatalog.Find(snapshot.DocumentFormKey);
            if (explicitForm is not null)
            {
                return explicitForm;
            }
        }

        if (!string.IsNullOrWhiteSpace(snapshot.ScenarioKey))
        {
            if (GuaranteeDocumentFormCatalog.IsScenarioSupported(snapshot.ScenarioKey))
            {
                if (!string.IsNullOrWhiteSpace(snapshot.BankName))
                {
                    return GuaranteeDocumentFormCatalog.ResolveDetectedForm(snapshot.ScenarioKey, snapshot.BankName);
                }

                return GuaranteeDocumentFormCatalog.GetDefaultForm(snapshot.ScenarioKey);
            }
        }

        return GuaranteeDocumentFormCatalog.GetFallbackForm(documentType);
    }

    private static string? TryReadString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? Normalize(property.GetString())
            : null;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
