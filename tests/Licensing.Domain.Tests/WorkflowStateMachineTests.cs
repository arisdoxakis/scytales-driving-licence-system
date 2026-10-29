using Licensing.Domain.Applications;
using Licensing.Domain.Workflow;
using static Licensing.Domain.Applications.ApplicationStatus;
using static Licensing.Domain.Applications.WorkflowTrigger;

namespace Licensing.Domain.Tests;

public class WorkflowStateMachineTests
{
    // Written out independently of the production table, on purpose.
    private static readonly Dictionary<(ApplicationStatus, WorkflowTrigger), ApplicationStatus> Expected = new()
    {
        [(Submitted, ValidationStarted)] = ValidatingData,
        [(ValidatingData, ValidationPassed)] = CheckingPhoto,
        [(CheckingPhoto, PhotoCheckPassed)] = RiskAssessment,
        [(RiskAssessment, AutoApproved)] = Approved,
        [(RiskAssessment, ManualReviewRequired)] = PendingManualReview,
        [(ValidatingData, ValidationFailed)] = Failed,
        [(CheckingPhoto, PhotoCheckFailed)] = Failed,
        [(PendingManualReview, ManuallyApproved)] = Approved,
        [(PendingManualReview, ManuallyRejected)] = Rejected,
        [(Submitted, ProcessingFailed)] = Failed,
        [(ValidatingData, ProcessingFailed)] = Failed,
        [(CheckingPhoto, ProcessingFailed)] = Failed,
        [(RiskAssessment, ProcessingFailed)] = Failed,
    };

    public static TheoryData<ApplicationStatus, WorkflowTrigger> AllCombinations()
    {
        var data = new TheoryData<ApplicationStatus, WorkflowTrigger>();
        foreach (var status in Enum.GetValues<ApplicationStatus>())
        foreach (var trigger in Enum.GetValues<WorkflowTrigger>())
            data.Add(status, trigger);
        return data;
    }

    [Theory]
    [MemberData(nameof(AllCombinations))]
    public void Only_listed_transitions_are_allowed(ApplicationStatus from, WorkflowTrigger trigger)
    {
        if (Expected.TryGetValue((from, trigger), out var to))
        {
            Assert.True(WorkflowStateMachine.CanFire(from, trigger));
            Assert.Equal(to, WorkflowStateMachine.Next(from, trigger));
        }
        else
        {
            Assert.False(WorkflowStateMachine.CanFire(from, trigger));
            var ex = Assert.Throws<InvalidTransitionException>(() => WorkflowStateMachine.Next(from, trigger));
            Assert.Equal(from, ex.From);
            Assert.Equal(trigger, ex.Trigger);
        }
    }

    [Fact]
    public void Table_has_exactly_the_expected_number_of_transitions()
    {
        Assert.Equal(Expected.Count, WorkflowStateMachine.AllowedTransitions.Count);
    }

    [Theory]
    [InlineData(Approved)]
    [InlineData(Rejected)]
    [InlineData(Failed)]
    public void Terminal_states_have_no_outgoing_transitions(ApplicationStatus status)
    {
        Assert.True(WorkflowStateMachine.IsTerminal(status));
        Assert.DoesNotContain(WorkflowStateMachine.AllowedTransitions.Keys, k => k.From == status);
    }

    [Fact]
    public void ApplicationCreated_is_never_a_valid_trigger()
    {
        // It only appears as the first audit entry, written by Application.Submit.
        Assert.DoesNotContain(WorkflowStateMachine.AllowedTransitions.Keys, k => k.Trigger == ApplicationCreated);
    }
}