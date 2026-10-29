using Licensing.Contracts;
using Licensing.Domain.Workflow;
using Licensing.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Licensing.Worker.Consumers;

public sealed class ApplicationSubmittedConsumer(LicensingDbContext db, ApplicationWorkflow workflow, ILogger<ApplicationSubmittedConsumer> logger) : IConsumer<ApplicationSubmitted>
{
    public async Task Consume(ConsumeContext<ApplicationSubmitted> context)
    {
        var applicationId = context.Message.ApplicationId;
        using var scope = logger.BeginScope(new Dictionary<string, object> { ["ApplicationId"] = applicationId });

        var application = await db.Applications
            .SingleOrDefaultAsync(a => a.Id == applicationId, context.CancellationToken);

        if (application is null)
        {
            logger.LogWarning("Application not found, message ignored");
            return;
        }

        while (await workflow.TryAdvanceAsync(application, context.CancellationToken))
        {
            await db.SaveChangesAsync(context.CancellationToken);
            logger.LogInformation("Application moved to {Status}", application.Status);
        }
    }
}