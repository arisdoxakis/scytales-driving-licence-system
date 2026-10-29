using System;

namespace Licensing.Domain.Applications;

public sealed class AuditEntry
{
    public const string SystemActor = "system";

    public Guid Id { get; private set; }
    public Guid ApplicationId { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }
    public ApplicationStatus? FromStatus { get; private set; }
    public ApplicationStatus ToStatus { get; private set; }
    public WorkflowTrigger Event { get; private set; }
    public string? Details { get; private set; }
    public string Actor { get; private set; } = SystemActor;

    private AuditEntry() { }

    internal AuditEntry(Guid applicationId, DateTimeOffset timestamp, ApplicationStatus? fromStatus, ApplicationStatus toStatus, WorkflowTrigger @event, string? details, string actor)
    {
        Id = Guid.NewGuid();
        ApplicationId = applicationId;
        Timestamp = timestamp;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        Event = @event;
        Details = details;
        Actor = actor;
    }
}
