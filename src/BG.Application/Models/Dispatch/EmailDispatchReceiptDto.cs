namespace BG.Application.Models.Dispatch;

public sealed record EmailDispatchReceiptDto(string MessageId, DateTimeOffset SentAtUtc);
