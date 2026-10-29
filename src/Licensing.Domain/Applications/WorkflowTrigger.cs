namespace Licensing.Domain.Applications;

public enum WorkflowTrigger
{
    ApplicationCreated,
    ValidationStarted,
    ValidationPassed,
    ValidationFailed,
    PhotoCheckPassed,
    PhotoCheckFailed,
    AutoApproved,
    ManualReviewRequired,
    ManuallyApproved,
    ManuallyRejected,
    ProcessingFailed
}
