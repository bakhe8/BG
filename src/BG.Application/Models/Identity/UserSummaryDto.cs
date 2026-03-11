namespace BG.Application.Models.Identity;

public sealed record UserSummaryDto(
    Guid Id,
    string Username,
    string DisplayName,
    string? Email,
    string? ExternalId,
    string SourceType,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<string> Roles);
