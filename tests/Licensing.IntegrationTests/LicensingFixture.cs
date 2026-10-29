using Licensing.Api.Controllers;
using Licensing.Domain.Workflow;
using Licensing.Worker;
using MassTransit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace Licensing.IntegrationTests;

public sealed class LicensingFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().WithImage("postgres:17").Build();
    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder().WithImage("rabbitmq:4-management").Build();
    private readonly string _photoRoot = Path.Combine(Path.GetTempPath(), "licensing-tests", Guid.NewGuid().ToString("N"));

    private WebApplicationFactory<ApplicationsController>? _api;
    private IHost? _worker;

    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _rabbitMq.StartAsync());

        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:licensing-db"] = _postgres.GetConnectionString(),
            ["ConnectionStrings:messaging"] = _rabbitMq.GetConnectionString(),
            ["PhotoStorage:RootPath"] = _photoRoot,
        };

        // 1. API: starting it runs the migrations
        _api = new WebApplicationFactory<ApplicationsController>().WithWebHostBuilder(builder =>
        {
            foreach (var (key, value) in settings)
                builder.UseSetting(key, value);
        });
        Client = _api.CreateClient();

        // 2. Worker: same registration as its Program.cs; creates the queues
        var workerBuilder = Host.CreateApplicationBuilder();
        workerBuilder.Configuration.AddInMemoryCollection(settings);
        workerBuilder.Services.AddWorker(workerBuilder.Configuration);
        workerBuilder.Services.AddSingleton<IPhotoQualityAnalyzer>(new FixedPhotoQualityAnalyzer(0.9));
        workerBuilder.Services.Configure<MassTransitHostOptions>(o => o.WaitUntilStarted = true);

        _worker = workerBuilder.Build();
        await _worker.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_worker is not null)
        {
            await _worker.StopAsync();
            _worker.Dispose();
        }

        if (_api is not null)
            await _api.DisposeAsync();

        await _postgres.DisposeAsync();
        await _rabbitMq.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class LicensingCollection : ICollectionFixture<LicensingFixture>
{
    public const string Name = "Licensing";
}