using System.Globalization;
using System.Text.RegularExpressions;
using BG.Application.Intake;

namespace BG.Application.Services;

internal sealed partial class LocalIntakeCandidateValidator : IIntakeCandidateValidator
{
    private const decimal MinAmountSar = 1_000m;
    private const decimal MaxAmountSar = 500_000_000m;

    public IReadOnlyList<IntakeExtractionFieldCandidate> Validate(
        IEnumerable<IntakeExtractionFieldCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        var list = candidates.ToList();
        var validated = list.Select(Validate).ToArray();

        return ApplyCrossFieldRules(validated);
    }

    private static IntakeExtractionFieldCandidate Validate(IntakeExtractionFieldCandidate candidate)
    {
        return candidate.FieldKey switch
        {
            IntakeFieldKeys.GuaranteeNumber => ValidateGuaranteeNumber(candidate),
            IntakeFieldKeys.Amount          => ValidateAmount(candidate),
            IntakeFieldKeys.IssueDate       => ValidateDate(candidate),
            IntakeFieldKeys.ExpiryDate      => ValidateDate(candidate),
            IntakeFieldKeys.NewExpiryDate   => ValidateDate(candidate),
            IntakeFieldKeys.OfficialLetterDate => ValidateDate(candidate),
            IntakeFieldKeys.CurrencyCode    => ValidateCurrencyCode(candidate),
            _                               => candidate
        };
    }

    private static IntakeExtractionFieldCandidate ValidateGuaranteeNumber(IntakeExtractionFieldCandidate candidate)
    {
        if (GuaranteeNumberRegex().IsMatch(candidate.Value))
            return candidate;

        return candidate with
        {
            IsValid = false,
            ValidationMessage = "guarantee-number-format-invalid"
        };
    }

    private static IntakeExtractionFieldCandidate ValidateAmount(IntakeExtractionFieldCandidate candidate)
    {
        var normalized = candidate.Value.Replace(",", "").Trim();
        if (!decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
        {
            return candidate with
            {
                IsValid = false,
                ValidationMessage = "amount-not-numeric"
            };
        }

        if (amount < MinAmountSar || amount > MaxAmountSar)
        {
            return candidate with
            {
                IsValid = false,
                ValidationMessage = "amount-out-of-range"
            };
        }

        return candidate;
    }

    private static IntakeExtractionFieldCandidate ValidateDate(IntakeExtractionFieldCandidate candidate)
    {
        if (TryParseDate(candidate.Value, out _))
            return candidate;

        return candidate with
        {
            IsValid = false,
            ValidationMessage = "date-format-invalid"
        };
    }

    private static IntakeExtractionFieldCandidate ValidateCurrencyCode(IntakeExtractionFieldCandidate candidate)
    {
        var upper = candidate.Value.Trim().ToUpperInvariant();
        if (upper.Length == 3 && upper.All(char.IsAsciiLetter))
            return candidate;

        return candidate with
        {
            IsValid = false,
            ValidationMessage = "currency-code-invalid"
        };
    }

    private static IReadOnlyList<IntakeExtractionFieldCandidate> ApplyCrossFieldRules(
        IntakeExtractionFieldCandidate[] candidates)
    {
        var byKey = candidates.ToDictionary(c => c.FieldKey, StringComparer.Ordinal);

        var result = candidates.ToList();

        // ExpiryDate must be after IssueDate
        if (byKey.TryGetValue(IntakeFieldKeys.IssueDate, out var issueDate) &&
            byKey.TryGetValue(IntakeFieldKeys.ExpiryDate, out var expiryDate) &&
            issueDate.IsValid && expiryDate.IsValid &&
            TryParseDate(issueDate.Value, out var issued) &&
            TryParseDate(expiryDate.Value, out var expires) &&
            expires <= issued)
        {
            UpdateInList(result, expiryDate with
            {
                IsValid = false,
                ValidationMessage = "expiry-not-after-issue"
            });
        }

        // NewExpiryDate must be after ExpiryDate when both present
        if (byKey.TryGetValue(IntakeFieldKeys.ExpiryDate, out var currentExpiry) &&
            byKey.TryGetValue(IntakeFieldKeys.NewExpiryDate, out var newExpiry) &&
            currentExpiry.IsValid && newExpiry.IsValid &&
            TryParseDate(currentExpiry.Value, out var current) &&
            TryParseDate(newExpiry.Value, out var extended) &&
            extended <= current)
        {
            UpdateInList(result, newExpiry with
            {
                IsValid = false,
                ValidationMessage = "new-expiry-not-after-current-expiry"
            });
        }

        return result;
    }

    private static void UpdateInList(List<IntakeExtractionFieldCandidate> list, IntakeExtractionFieldCandidate updated)
    {
        var index = list.FindIndex(c => c.FieldKey == updated.FieldKey && c.Source == updated.Source);
        if (index >= 0)
            list[index] = updated;
    }

    private static bool TryParseDate(string value, out DateOnly date)
    {
        return DateOnly.TryParseExact(value, ["yyyy-MM-dd", "dd/MM/yyyy", "MM/dd/yyyy"],
            CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    [GeneratedRegex(@"^BG-\d{4}-\d{3,6}$", RegexOptions.IgnoreCase)]
    private static partial Regex GuaranteeNumberRegex();
}
