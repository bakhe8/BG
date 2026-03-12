using BG.Domain.Guarantees;

namespace BG.Application.Requests;

public static class RequestAccessPolicy
{
    public static bool CanViewRequest(Guid currentUserId, GuaranteeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.IsOwnedBy(currentUserId);
    }

    public static bool CanViewRequestEvent(Guid currentUserId, GuaranteeRequest request, GuaranteeEvent guaranteeEvent)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(guaranteeEvent);

        return request.IsOwnedBy(currentUserId) &&
               guaranteeEvent.GuaranteeRequestId == request.Id;
    }
}
