using Licensing.Domain.Workflow;

namespace Licensing.Domain.Applications;

public sealed class Application
{
    private readonly List<AuditEntry> _history = [];

    public Guid Id { get; private set; }
    public PersonalDetails PersonalDetails { get; private set; } = null!;
    public LicenceCategory? LicenceCategory { get; private set; }
    public Photo? Photo { get; private set; }
    public ApplicationStatus Status { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public uint Version { get; private set; }

    public IReadOnlyCollection<AuditEntry> History => _history.AsReadOnly();

    public bool IsTerminal => WorkflowStateMachine.IsTerminal(Status);

    private Application() { }

    public static Application Submit(PersonalDetails personalDetails, LicenceCategory? licenceCategory, Photo? photo, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(personalDetails);

        var application = new Application
        {
            Id = Guid.NewGuid(),
            PersonalDetails = personalDetails,
            LicenceCategory = licenceCategory,
            Photo = photo,
            Status = ApplicationStatus.Submitted,
            CreatedAt = now,
            UpdatedAt = now,
        };

        application._history.Add(new AuditEntry(
            application.Id, now, fromStatus: null, ApplicationStatus.Submitted,
            WorkflowTrigger.ApplicationCreated, details: null, AuditEntry.SystemActor));

        return application;
    }

    public void Fire(WorkflowTrigger trigger, DateTimeOffset now, string? details = null, string actor = AuditEntry.SystemActor)
    {
        var from = Status;
        var to = WorkflowStateMachine.Next(from, trigger);

        Status = to;
        UpdatedAt = now;
        _history.Add(new AuditEntry(Id, now, from, to, trigger, details, actor));
    }

    public void RecordPhotoQuality(double score)
    {
        if (Photo is null)
            throw new InvalidOperationException("Cannot score a photo that was not provided.");
        if (score is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(score), "Score must be between 0 and 1.");

        Photo = Photo with { QualityScore = score };
    }

    public void Approve(string reviewer, DateTimeOffset now) => Fire(WorkflowTrigger.ManuallyApproved, now, details: null, reviewer);

    public void Reject(string reviewer, string? reason, DateTimeOffset now)
    {
        Fire(WorkflowTrigger.ManuallyRejected, now, reason, reviewer);
        RejectionReason = reason;
    }
}