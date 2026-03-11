namespace BG.Domain.Guarantees;

public enum GuaranteeRequestType
{
    Extend = 1,
    Reduce = 2,
    Release = 3,
    ReplaceWithReducedGuarantee = 4,
    VerifyStatus = 5
}
