using BG.Domain.Guarantees;

namespace BG.Application.Models.Administration;

public sealed record CreateBankCommand(
    string CanonicalName,
    string ShortCode,
    string? OfficialEmail,
    bool IsEmailDispatchEnabled,
    IReadOnlyList<GuaranteeDispatchChannel> SupportedDispatchChannels,
    string? Notes);
