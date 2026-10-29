using Licensing.Domain.Applications;
using Licensing.Domain.Rules;
using static Licensing.Domain.Applications.ApplicationStatus;
using static Licensing.Domain.Applications.WorkflowTrigger;

namespace Licensing.Domain.Workflow;

public sealed class ApplicationWorkflow(DataValidator dataValidator, PhotoChecker photoChecker, RiskAssessor riskAssessor, IPhotoQualityAnalyzer photoQualityAnalyzer, TimeProvider timeProvider)
{
    /// <summary>
    /// Performs exactly one transition, chosen from the current status.
    /// Returns false when nothing automated is left to do.
    /// </summary>
    public async Task<bool> TryAdvanceAsync(Application application, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        switch (application.Status)
        {
            case Submitted:
                application.Fire(ValidationStarted, now);
                return true;

            case ValidatingData:
            {
                var result = dataValidator.Validate(application, today);
                application.Fire(result.Passed ? ValidationPassed : ValidationFailed, now, result.Details);
                return true;
            }

            case CheckingPhoto:
            {
                // Score only once; a retry reuses the stored score.
                if (application.Photo is { QualityScore: null } photo)
                    application.RecordPhotoQuality(await photoQualityAnalyzer.AnalyzeAsync(photo, cancellationToken));

                var result = photoChecker.Check(application.Photo);
                application.Fire(result.Passed ? PhotoCheckPassed : PhotoCheckFailed, now, result.Details);
                return true;
            }

            case RiskAssessment:
            {
                var result = riskAssessor.Assess(application, today);
                application.Fire(result.Passed ? AutoApproved : ManualReviewRequired, now, result.Details);
                return true;
            }

            default:
                return false; // PendingManualReview or a terminal status
        }
    }
}