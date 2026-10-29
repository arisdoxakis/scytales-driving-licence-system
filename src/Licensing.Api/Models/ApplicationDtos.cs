using Licensing.Domain.Applications;

namespace Licensing.Api.Endpoints;

public sealed class SubmitApplicationRequest
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public string? NationalId { get; init; }
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Address { get; init; }
    public LicenceCategory? LicenceCategory { get; init; }
    public IFormFile? Photo { get; init; }
}

public sealed record SubmitApplicationResponse(Guid ApplicationId, ApplicationStatus Status);

public sealed record ApplicationStatusResponse(
    Guid ApplicationId,
    ApplicationStatus Status,
    string? RejectionReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record HistoryEntryResponse(
    DateTimeOffset Timestamp,
    ApplicationStatus? FromStatus,
    ApplicationStatus ToStatus,
    WorkflowTrigger Event,
    string? Details,
    string Actor);

public sealed record RejectApplicationRequest(string? Reason);