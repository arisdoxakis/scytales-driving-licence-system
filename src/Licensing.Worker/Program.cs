using Licensing.Worker;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.AddServiceDefaults();

builder.Services.AddSerilog((services, logger) => logger
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext(),
    writeToProviders: true);

builder.Services.AddWorker(builder.Configuration);

await builder.Build().RunAsync();