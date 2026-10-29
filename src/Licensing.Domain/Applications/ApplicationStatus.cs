namespace Licensing.Domain.Applications;

public enum ApplicationStatus
{
    Submitted,
    ValidatingData,
    CheckingPhoto,
    RiskAssessment,
    PendingManualReview,
    Approved,
    Rejected,
    Failed
}
