using Licensing.Domain.Applications;

namespace Licensing.Domain.Workflow;

public sealed class InvalidTransitionException(ApplicationStatus from, WorkflowTrigger trigger) : DomainException($"Cannot apply '{trigger}' to an application in status '{from}'.")
{
    public ApplicationStatus From { get; } = from;
    public WorkflowTrigger Trigger { get; } = trigger;
}