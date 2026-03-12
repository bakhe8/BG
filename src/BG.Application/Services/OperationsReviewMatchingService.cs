using System.Text.Json;
using BG.Application.Contracts.Services;
using BG.Application.Intake;
using BG.Application.Operations;
using BG.Application.ReferenceData;
using BG.Domain.Guarantees;
using BG.Domain.Operations;

namespace BG.Application.Services;

internal sealed class OperationsReviewMatchingService : IOperationsReviewMatchingService
{
    public IReadOnlyList<OperationsReviewMatchSuggestionDto> SuggestMatches(OperationsReviewQueueItemReadModel item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (!SupportsMatching(item))
        {
            return [];
        }

        var scenario = IntakeScenarioCatalog.Find(item.ScenarioKey);
        if (scenario?.ExpectedRequestType is not { } expectedRequestType)
        {
            return [];
        }

        var compatibleCandidates = item.CandidateRequests
            .Where(request => request.RequestType == expectedRequestType && IsOpenForBankResponse(request.Status))
            .ToArray();

        if (compatibleCandidates.Length == 0)
        {
            return [];
        }

        var responseSignal = ParseResponseSignal(item);

        return compatibleCandidates
            .Select(request => CreateSuggestion(request, responseSignal, compatibleCandidates.Length == 1))
            .OrderByDescending(suggestion => suggestion.Score)
            .ThenByDescending(suggestion => suggestion.SubmittedToBankAtUtc)
            .ToArray();
    }

    internal static bool SupportsMatching(OperationsReviewQueueItemReadModel item)
    {
        return IntakeScenarioCatalog.Find(item.ScenarioKey)?.SupportsRequestMatching == true &&
               item.Category is (OperationsReviewItemCategory.IncomingBankConfirmation or OperationsReviewItemCategory.IncomingStatusReply);
    }

    private static OperationsReviewMatchSuggestionDto CreateSuggestion(
        OperationsReviewRequestCandidateReadModel request,
        ResponseSignal responseSignal,
        bool isOnlyCompatibleCandidate)
    {
        var score = 40;
        var reasons = new List<string>
        {
            "OperationsMatchReason_RequestTypeAligned"
        };

        switch (request.Status)
        {
            case GuaranteeRequestStatus.AwaitingBankResponse:
                score += 25;
                reasons.Add("OperationsMatchReason_WaitingForBankResponse");
                break;
            case GuaranteeRequestStatus.SubmittedToBank:
                score += 20;
                reasons.Add("OperationsMatchReason_WaitingForBankResponse");
                break;
            case GuaranteeRequestStatus.ApprovedForDispatch:
                score += 10;
                reasons.Add("OperationsMatchReason_ApprovedForDispatch");
                break;
        }

        if (isOnlyCompatibleCandidate)
        {
            score += 20;
            reasons.Add("OperationsMatchReason_OnlyOpenCandidate");
        }

        if (responseSignal.LetterDate.HasValue && request.SubmittedToBankAtUtc.HasValue)
        {
            var submittedDate = DateOnly.FromDateTime(request.SubmittedToBankAtUtc.Value.UtcDateTime);
            var dayDelta = Math.Abs(responseSignal.LetterDate.Value.DayNumber - submittedDate.DayNumber);

            if (responseSignal.LetterDate.Value.DayNumber >= submittedDate.DayNumber)
            {
                score += 10;
                reasons.Add("OperationsMatchReason_ResponseAfterDispatch");
            }

            if (dayDelta <= 14)
            {
                score += 10;
                reasons.Add("OperationsMatchReason_ResponseDateNearDispatch");
            }
            else if (dayDelta <= 45)
            {
                score += 5;
                reasons.Add("OperationsMatchReason_ResponseDateNearDispatch");
            }
        }

        if (responseSignal.ExpectedRequestType == GuaranteeRequestType.Extend &&
            responseSignal.NewExpiryDate.HasValue &&
            request.RequestedExpiryDate == responseSignal.NewExpiryDate)
        {
            score += 20;
            reasons.Add("OperationsMatchReason_ExpiryDateMatches");
        }

        if (responseSignal.ExpectedRequestType == GuaranteeRequestType.Reduce &&
            responseSignal.ConfirmedAmount.HasValue &&
            request.RequestedAmount.HasValue &&
            decimal.Round(responseSignal.ConfirmedAmount.Value, 2, MidpointRounding.AwayFromZero) ==
            decimal.Round(request.RequestedAmount.Value, 2, MidpointRounding.AwayFromZero))
        {
            score += 20;
            reasons.Add("OperationsMatchReason_AmountMatches");
        }

        score = Math.Min(score, 99);

        return new OperationsReviewMatchSuggestionDto(
            request.RequestId,
            GuaranteeResourceCatalog.GetRequestTypeResourceKey(request.RequestType),
            GuaranteeResourceCatalog.GetRequestStatusResourceKey(request.Status),
            score,
            MapConfidence(score),
            request.SubmittedToBankAtUtc,
            request.LatestOutgoingReferenceNumber,
            reasons);
    }

    private static ResponseSignal ParseResponseSignal(OperationsReviewQueueItemReadModel item)
    {
        var letterDate = item.BankLetterDate;
        DateOnly? newExpiryDate = null;
        decimal? confirmedAmount = null;

        if (!string.IsNullOrWhiteSpace(item.VerifiedDataJson))
        {
            try
            {
                using var document = JsonDocument.Parse(item.VerifiedDataJson);

                if (document.RootElement.TryGetProperty(IntakeVerifiedDataKeys.NewExpiryDate, out var newExpiryDateProperty) &&
                    newExpiryDateProperty.ValueKind == JsonValueKind.String &&
                    DateOnly.TryParse(newExpiryDateProperty.GetString(), out var parsedExpiryDate))
                {
                    newExpiryDate = parsedExpiryDate;
                }

                if (document.RootElement.TryGetProperty(IntakeVerifiedDataKeys.Amount, out var amountProperty) &&
                    amountProperty.ValueKind == JsonValueKind.String &&
                    TryParseAmount(amountProperty.GetString(), out var parsedAmount))
                {
                    confirmedAmount = parsedAmount;
                }
            }
            catch (JsonException)
            {
                newExpiryDate = null;
                confirmedAmount = null;
            }
        }

        return new ResponseSignal(IntakeScenarioCatalog.Find(item.ScenarioKey)?.ExpectedRequestType, letterDate, newExpiryDate, confirmedAmount);
    }

    private static bool IsOpenForBankResponse(GuaranteeRequestStatus status)
    {
        return status is GuaranteeRequestStatus.AwaitingBankResponse or GuaranteeRequestStatus.SubmittedToBank or GuaranteeRequestStatus.ApprovedForDispatch;
    }

    private static string MapConfidence(int score)
    {
        return score switch
        {
            >= 85 => "OperationsMatchConfidence_High",
            >= 65 => "OperationsMatchConfidence_Medium",
            _ => "OperationsMatchConfidence_Low"
        };
    }

    private static bool TryParseAmount(string? value, out decimal amount)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            amount = default;
            return false;
        }

        return decimal.TryParse(value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out amount)
               || decimal.TryParse(value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.CurrentCulture, out amount);
    }

    private sealed record ResponseSignal(
        GuaranteeRequestType? ExpectedRequestType,
        DateOnly? LetterDate,
        DateOnly? NewExpiryDate,
        decimal? ConfirmedAmount);
}
