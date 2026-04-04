using BG.Domain.Guarantees;

namespace BG.Application.Models.Administration;

public sealed record UpdateBankCommand(
    Guid BankId,
    string CanonicalName,
    string ShortCode,
    string? OfficialEmail,
    bool IsEmailDispatchEnabled,
    IReadOnlyList<GuaranteeDispatchChannel> SupportedDispatchChannels,
    string? Notes);
