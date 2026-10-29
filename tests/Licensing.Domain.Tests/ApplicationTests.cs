using Licensing.Domain.Applications;
using Licensing.Domain.Workflow;

namespace Licensing.Domain.Tests;

public class ApplicationTests
{
    [Fact]
    public void Submit_starts_in_Submitted_with_creation_audit_entry()
    {
        var app = TestData.NewApplication();

        Assert.Equal(ApplicationStatus.Submitted, app.Status);
        Assert.Equal(TestData.Now, app.CreatedAt);
        var entry = Assert.Single(app.History);
        Assert.Null(entry.FromStatus);
        Assert.Equal(ApplicationStatus.Submitted, entry.ToStatus);
        Assert.Equal(WorkflowTrigger.ApplicationCreated, entry.Event);
        Assert.Equal(app.Id, entry.ApplicationId);
    }

    [Fact]
    public void Fire_changes_status_and_records_transition()
    {
        var app = TestData.NewApplication();
        var later = TestData.Now.AddMinutes(1);

        app.Fire(WorkflowTrigger.ValidationStarted, later);

        Assert.Equal(ApplicationStatus.ValidatingData, app.Status);
        Assert.Equal(later, app.UpdatedAt);
        var entry = app.History.Last();
        Assert.Equal(ApplicationStatus.Submitted, entry.FromStatus);
        Assert.Equal(ApplicationStatus.ValidatingData, entry.ToStatus);
        Assert.Equal(WorkflowTrigger.ValidationStarted, entry.Event);
        Assert.Equal(AuditEntry.SystemActor, entry.Actor);
        Assert.Equal(later, entry.Timestamp);
    }

    [Fact]
    public void Invalid_trigger_throws_and_leaves_state_and_history_untouched()
    {
        var app = TestData.NewApplication();

        Assert.Throws<InvalidTransitionException>(() => app.Fire(WorkflowTrigger.AutoApproved, TestData.Now));

        Assert.Equal(ApplicationStatus.Submitted, app.Status);
        Assert.Single(app.History);
    }

    [Fact]
    public void Approve_from_manual_review_records_reviewer()
    {
        var app = TestData.InStatus(ApplicationStatus.PendingManualReview);

        app.Approve("reviewer-1", TestData.Now);

        Assert.Equal(ApplicationStatus.Approved, app.Status);
        var entry = app.History.Last();
        Assert.Equal(WorkflowTrigger.ManuallyApproved, entry.Event);
        Assert.Equal("reviewer-1", entry.Actor);
    }

    [Fact]
    public void Reject_from_manual_review_stores_reason_in_application_and_audit()
    {
        var app = TestData.InStatus(ApplicationStatus.PendingManualReview);

        app.Reject("reviewer-1", "Photo does not meet quality requirements", TestData.Now);

        Assert.Equal(ApplicationStatus.Rejected, app.Status);
        Assert.Equal("Photo does not meet quality requirements", app.RejectionReason);
        Assert.Equal("Photo does not meet quality requirements", app.History.Last().Details);
    }

    [Theory]
    [InlineData(ApplicationStatus.Submitted)]
    [InlineData(ApplicationStatus.RiskAssessment)]
    [InlineData(ApplicationStatus.Approved)]
    [InlineData(ApplicationStatus.Rejected)]
    [InlineData(ApplicationStatus.Failed)]
    public void Manual_decisions_outside_manual_review_are_rejected(ApplicationStatus status)
    {
        var app = TestData.InStatus(status);
        var historyCount = app.History.Count;

        Assert.Throws<InvalidTransitionException>(() => app.Approve("reviewer-1", TestData.Now));
        Assert.Throws<InvalidTransitionException>(() => app.Reject("reviewer-1", "no", TestData.Now));

        Assert.Equal(status, app.Status);
        Assert.Null(app.RejectionReason);
        Assert.Equal(historyCount, app.History.Count);
    }

    [Fact]
    public void ProcessingFailed_from_an_automated_step_records_the_error()
    {
        var app = TestData.InStatus(ApplicationStatus.CheckingPhoto);

        app.Fire(WorkflowTrigger.ProcessingFailed, TestData.Now, "Photo storage unavailable");

        Assert.Equal(ApplicationStatus.Failed, app.Status);
        Assert.Equal("Photo storage unavailable", app.History.Last().Details);
    }

    [Fact]
    public void RecordPhotoQuality_requires_a_photo()
    {
        var app = TestData.NewApplication(withoutPhoto: true);

        Assert.Throws<InvalidOperationException>(() => app.RecordPhotoQuality(0.5));
    }
}