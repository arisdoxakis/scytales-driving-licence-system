using Licensing.Infrastructure;
using Licensing.Worker.Consumers;

namespace Licensing.Worker;

public static class WorkerRegistration
{
    public static IServiceCollection AddWorker(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddInfrastructure(configuration);
        services.AddMessaging(configuration, x =>
        {
            x.AddConsumer<ApplicationSubmittedConsumer>();
            x.AddConsumer<ApplicationSubmittedFaultConsumer>();
        });
        return services;
    }
}