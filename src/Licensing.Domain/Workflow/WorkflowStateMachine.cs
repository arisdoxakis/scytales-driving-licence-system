using Licensing.Domain.Applications;
using static Licensing.Domain.Applications.ApplicationStatus;
using static Licensing.Domain.Applications.WorkflowTrigger;

namespace Licensing.Domain.Workflow;

public static class WorkflowStateMachine
{
    private static readonly Dictionary<(ApplicationStatus From, WorkflowTrigger Trigger), ApplicationStatus> Transitions = new()
    {
        // Success path
        [(Submitted,           ValidationStarted)]    = ValidatingData,
        [(ValidatingData,      ValidationPassed)]     = CheckingPhoto,
        [(CheckingPhoto,       PhotoCheckPassed)]     = RiskAssessment,
        [(RiskAssessment,      AutoApproved)]         = Approved,
        [(RiskAssessment,      ManualReviewRequired)] = PendingManualReview,

        // Business failures
        [(ValidatingData,      ValidationFailed)]     = Failed,
        [(CheckingPhoto,       PhotoCheckFailed)]     = Failed,

        // Manual review
        [(PendingManualReview, ManuallyApproved)]     = Approved,
        [(PendingManualReview, ManuallyRejected)]     = Rejected,

        // Technical failure after retries are exhausted (fault consumer)
        [(Submitted,           ProcessingFailed)]     = Failed,
        [(ValidatingData,      ProcessingFailed)]     = Failed,
        [(CheckingPhoto,       ProcessingFailed)]     = Failed,
        [(RiskAssessment,      ProcessingFailed)]     = Failed,
    };

    public static IReadOnlyDictionary<(ApplicationStatus From, WorkflowTrigger Trigger), ApplicationStatus> AllowedTransitions
        => Transitions;

    public static bool CanFire(ApplicationStatus from, WorkflowTrigger trigger)
        => Transitions.ContainsKey((from, trigger));

    public static ApplicationStatus Next(ApplicationStatus from, WorkflowTrigger trigger)
        => Transitions.TryGetValue((from, trigger), out var to)
            ? to
            : throw new InvalidTransitionException(from, trigger);

    public static bool IsTerminal(ApplicationStatus status)
        => status is Approved or Rejected or Failed;
}
