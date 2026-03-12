using BG.Domain.Operations;

namespace BG.Application.Operations;

internal static class OperationsReviewResourceCatalog
{
    public static string GetCategoryResourceKey(OperationsReviewItemCategory category)
    {
        return category switch
        {
            OperationsReviewItemCategory.GuaranteeRegistration => "OperationsReviewCategory_GuaranteeRegistration",
            OperationsReviewItemCategory.IncomingBankConfirmation => "OperationsReviewCategory_BankConfirmation",
            OperationsReviewItemCategory.IncomingStatusReply => "OperationsReviewCategory_StatusReply",
            OperationsReviewItemCategory.SupportingDocumentation => "OperationsReviewCategory_SupportingDocumentation",
            _ => "OperationsReviewCategory_GuaranteeRegistration"
        };
    }

    public static string GetRecommendedLaneResourceKey(OperationsReviewItemCategory category)
    {
        return category switch
        {
            OperationsReviewItemCategory.GuaranteeRegistration => "OperationsReviewLane_RegistrationReview",
            OperationsReviewItemCategory.IncomingBankConfirmation => "OperationsReviewLane_BankConfirmationReview",
            OperationsReviewItemCategory.IncomingStatusReply => "OperationsReviewLane_StatusAssessment",
            OperationsReviewItemCategory.SupportingDocumentation => "OperationsReviewLane_DocumentLinking",
            _ => "OperationsReviewLane_RegistrationReview"
        };
    }
}
