using Licensing.Api.Endpoints;
using Licensing.Contracts;
using Licensing.Domain.Applications;
using Licensing.Infrastructure.Persistence;
using Licensing.Infrastructure.Storage;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Licensing.Domain.Workflow;

namespace Licensing.Api.Controllers;

[ApiController]
[Route("api/applications")]
[Produces("application/json")]
public sealed class ApplicationsController(LicensingDbContext db, TimeProvider timeProvider) : ControllerBase
{
    private const string ReviewerHeader = "X-Reviewer-Id";

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5_242_880)]
    [RequestFormLimits(MultipartBodyLengthLimit = 5_242_880)]
    [ProducesResponseType<SubmitApplicationResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Submit([FromForm] SubmitApplicationRequest request, [FromServices] IPhotoStorage photoStorage, [FromServices] IPublishEndpoint publishEndpoint, CancellationToken ct)
    {
        Photo? photo = null;
        if (request.Photo is { Length: > 0 } file)
        {
            await using var stream = file.OpenReadStream();
            var path = await photoStorage.SaveAsync(stream, file.ContentType, ct);
            photo = new Photo(path, file.ContentType, file.Length);
        }

        var details = new PersonalDetails(
            request.FirstName, request.LastName, request.DateOfBirth, request.NationalId,
            request.Email, request.PhoneNumber, request.Address);

        var application = Application.Submit(details, request.LicenceCategory, photo, timeProvider.GetUtcNow());

        db.Applications.Add(application);
        await publishEndpoint.Publish(new ApplicationSubmitted(application.Id), ct);
        await db.SaveChangesAsync(ct);

        return AcceptedAtAction(
            nameof(GetStatus),
            new { applicationId = application.Id },
            new SubmitApplicationResponse(application.Id, application.Status));
    }

    [HttpGet("{applicationId:guid}")]
    [ProducesResponseType<ApplicationStatusResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApplicationStatusResponse>> GetStatus(Guid applicationId, CancellationToken ct)
    {
        var response = await db.Applications
            .AsNoTracking()
            .Where(a => a.Id == applicationId)
            .Select(a => new ApplicationStatusResponse(a.Id, a.Status, a.RejectionReason, a.CreatedAt, a.UpdatedAt))
            .SingleOrDefaultAsync(ct);

        return response is null ? NotFound() : Ok(response);
    }

    [HttpGet("{applicationId:guid}/history")]
    [ProducesResponseType<List<HistoryEntryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<HistoryEntryResponse>>> GetHistory(Guid applicationId, CancellationToken ct)
    {
        if (!await db.Applications.AnyAsync(a => a.Id == applicationId, ct))
            return NotFound();

        var history = await db.AuditEntries
            .AsNoTracking()
            .Where(e => e.ApplicationId == applicationId)
            .OrderBy(e => e.Timestamp)
            .Select(e => new HistoryEntryResponse(e.Timestamp, e.FromStatus, e.ToStatus, e.Event, e.Details, e.Actor))
            .ToListAsync(ct);

        return Ok(history);
    }

    [HttpPost("{applicationId:guid}/approve")]
    [ProducesResponseType<ApplicationStatusResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApplicationStatusResponse>> Approve(Guid applicationId, [FromHeader(Name = ReviewerHeader)] string? reviewerId, CancellationToken ct)
    {
        var application = await db.Applications.SingleOrDefaultAsync(a => a.Id == applicationId, ct);
        if (application is null)
            return NotFound();

        if (!WorkflowStateMachine.CanFire(application.Status, WorkflowTrigger.ManuallyApproved))
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Invalid state transition",
                detail: $"Cannot approve an application in status '{application.Status}'.");

        application.Approve(reviewerId ?? "unknown-reviewer", timeProvider.GetUtcNow());
        await db.SaveChangesAsync(ct);

        return Ok(ToStatusResponse(application));
    }

    [HttpPost("{applicationId:guid}/reject")]
    [Consumes("application/json")]
    [ProducesResponseType<ApplicationStatusResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApplicationStatusResponse>> Reject(Guid applicationId, [FromBody] RejectApplicationRequest? request, [FromHeader(Name = ReviewerHeader)] string? reviewerId, CancellationToken ct)
    {
        var application = await db.Applications.SingleOrDefaultAsync(a => a.Id == applicationId, ct);
        if (application is null)
            return NotFound();

        if (!WorkflowStateMachine.CanFire(application.Status, WorkflowTrigger.ManuallyRejected))
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Invalid state transition",
                detail: $"Cannot reject an application in status '{application.Status}'.");

        application.Reject(reviewerId ?? "unknown-reviewer", request?.Reason, timeProvider.GetUtcNow());
        await db.SaveChangesAsync(ct);

        return Ok(ToStatusResponse(application));
    }

    private static ApplicationStatusResponse ToStatusResponse(Application a) => new(a.Id, a.Status, a.RejectionReason, a.CreatedAt, a.UpdatedAt);
}