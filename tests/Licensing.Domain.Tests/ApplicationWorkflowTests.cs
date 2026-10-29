using Licensing.Domain.Applications;
using Licensing.Domain.Rules;
using Licensing.Domain.Workflow;

namespace Licensing.Domain.Tests;

public class ApplicationWorkflowTests
{
    private readonly FakePhotoAnalyzer _analyzer = new();
    private readonly ApplicationWorkflow _workflow;

    public ApplicationWorkflowTests()
    {
        _workflow = new ApplicationWorkflow(
            new DataValidator(),
            new PhotoChecker(new PhotoCheckOptions()),
            new RiskAssessor(new RiskOptions()),
            _analyzer,
            new FixedTimeProvider(TestData.Now));
    }

    private async Task RunToCompletion(Application app)
    {
        var guard = 0;
        while (await _workflow.TryAdvanceAsync(app))
            Assert.True(++guard < 10, "Workflow did not settle");
    }

    private static ApplicationStatus[] StatusPath(Application app) =>
        app.History.Select(h => h.ToStatus).ToArray();

    [Fact]
    public async Task Low_risk_application_is_auto_approved()
    {
        _analyzer.Score = 0.9;
        var app = TestData.NewApplication(photo: TestData.ValidPhoto(qualityScore: null));

        await RunToCompletion(app);

        Assert.Equal(
            new[] { ApplicationStatus.Submitted, ApplicationStatus.ValidatingData, ApplicationStatus.CheckingPhoto,
                    ApplicationStatus.RiskAssessment, ApplicationStatus.Approved },
            StatusPath(app));
        Assert.Equal(0.9, app.Photo!.QualityScore);
    }

    [Fact]
    public async Task Young_applicant_stops_at_manual_review_with_reason()
    {
        var app = TestData.NewApplication(TestData.ValidDetails(TestData.Today.AddYears(-19)));

        await RunToCompletion(app);

        Assert.Equal(ApplicationStatus.PendingManualReview, app.Status);
        var entry = app.History.Last();
        Assert.Equal(WorkflowTrigger.ManualReviewRequired, entry.Event);
        Assert.Equal("Applicant is below 21", entry.Details);
    }

    [Fact]
    public async Task Invalid_data_fails_without_checking_photo()
    {
        var app = TestData.NewApplication(TestData.ValidDetails() with { Email = "bad" });

        await RunToCompletion(app);

        Assert.Equal(
            new[] { ApplicationStatus.Submitted, ApplicationStatus.ValidatingData, ApplicationStatus.Failed },
            StatusPath(app));
        Assert.Equal("Email is invalid", app.History.Last().Details);
        Assert.Equal(0, _analyzer.Calls);
    }

    [Fact]
    public async Task Missing_photo_fails_at_photo_check()
    {
        var app = TestData.NewApplication(withoutPhoto: true);

        await RunToCompletion(app);

        Assert.Equal(ApplicationStatus.Failed, app.Status);
        Assert.Equal(WorkflowTrigger.PhotoCheckFailed, app.History.Last().Event);
    }

    [Fact]
    public async Task Resumes_from_current_status_and_reuses_existing_photo_score()
    {
        // As if the worker crashed after scoring the photo but before the next save.
        var app = TestData.InStatus(ApplicationStatus.CheckingPhoto);

        await RunToCompletion(app);

        Assert.Equal(ApplicationStatus.Approved, app.Status);
        Assert.Equal(0, _analyzer.Calls);
    }

    [Theory]
    [InlineData(ApplicationStatus.PendingManualReview)]
    [InlineData(ApplicationStatus.Approved)]
    [InlineData(ApplicationStatus.Rejected)]
    [InlineData(ApplicationStatus.Failed)]
    public async Task Duplicate_processing_after_completion_is_a_no_op(ApplicationStatus status)
    {
        var app = TestData.InStatus(status);
        var historyCount = app.History.Count;

        Assert.False(await _workflow.TryAdvanceAsync(app));
        Assert.Equal(historyCount, app.History.Count);
    }

    private sealed class FakePhotoAnalyzer : IPhotoQualityAnalyzer
    {
        public double Score { get; set; } = 0.9;
        public int Calls { get; private set; }

        public Task<double> AnalyzeAsync(Photo photo, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(Score);
        }
    }
}