using Licensing.Contracts;
using Licensing.Domain.Applications;
using Licensing.Domain.Workflow;
using Licensing.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Licensing.Worker.Consumers;

public sealed class ApplicationSubmittedFaultConsumer(LicensingDbContext db, TimeProvider timeProvider, ILogger<ApplicationSubmittedFaultConsumer> logger) : IConsumer<Fault<ApplicationSubmitted>>
{
    private const int MaxReasonLength = 500;

    public async Task Consume(ConsumeContext<Fault<ApplicationSubmitted>> context)
    {
        var applicationId = context.Message.Message.ApplicationId;
        var error = context.Message.Exceptions.FirstOrDefault()?.Message ?? "Unknown error";
        if (error.Length > MaxReasonLength)
            error = error[..MaxReasonLength];

        var application = await db.Applications
            .SingleOrDefaultAsync(a => a.Id == applicationId, context.CancellationToken);

        if (application is null || !WorkflowStateMachine.CanFire(application.Status, WorkflowTrigger.ProcessingFailed))
            return; // already finished or already marked; nothing to do

        application.Fire(WorkflowTrigger.ProcessingFailed, timeProvider.GetUtcNow(),
            $"Processing failed after retries: {error}");
        await db.SaveChangesAsync(context.CancellationToken);

        logger.LogError("Application {ApplicationId} marked Failed after retries were exhausted: {Error}",
            applicationId, error);
    }
}