using Licensing.Domain;
using Licensing.Domain.Rules;
using Licensing.Domain.Workflow;
using Licensing.Infrastructure.Persistence;
using Licensing.Infrastructure.Storage;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Licensing.Infrastructure;

public static class DependencyInjection
{
    public const string DatabaseConnectionName = "licensing-db";
    public const string MessagingConnectionName = "messaging";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(DatabaseConnectionName)
            ?? throw new InvalidOperationException($"Connection string '{DatabaseConnectionName}' is missing.");

        services.AddDbContext<LicensingDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());

        services.AddBoundOptions<PhotoCheckOptions>(configuration, PhotoCheckOptions.SectionName);
        services.AddBoundOptions<RiskOptions>(configuration, RiskOptions.SectionName);
        services.AddBoundOptions<PhotoStorageOptions>(configuration, PhotoStorageOptions.SectionName);

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IPhotoStorage, LocalFilePhotoStorage>();
        services.AddSingleton<IPhotoQualityAnalyzer, PhotoQualityAnalyzer>();

        services.AddSingleton<DataValidator>();
        services.AddSingleton<PhotoChecker>();
        services.AddSingleton<RiskAssessor>();
        services.AddScoped<ApplicationWorkflow>();

        return services;
    }

    public static IServiceCollection AddMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configureConsumers = null)
    {
        var rabbitConnectionString = configuration.GetConnectionString(MessagingConnectionName)
            ?? throw new InvalidOperationException($"Connection string '{MessagingConnectionName}' is missing.");

        services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();

            x.AddEntityFrameworkOutbox<LicensingDbContext>(o =>
            {
                o.UsePostgres();
                o.UseBusOutbox();
            });

            configureConsumers?.Invoke(x);

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(new Uri(rabbitConnectionString));

                cfg.UseMessageRetry(r =>
                {
                    r.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(3));
                    r.Ignore<DomainException>();
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }

    private static void AddBoundOptions<T>(this IServiceCollection services, IConfiguration configuration, string section)
        where T : class
    {
        services.AddOptions<T>().Bind(configuration.GetSection(section));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<T>>().Value);
    }
}