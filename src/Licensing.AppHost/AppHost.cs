var builder = DistributedApplication.CreateBuilder(args);

var database = builder.AddPostgres("postgres")
    .WithPgAdmin()
    .WithDataVolume()
    .AddDatabase("licensing-db");

var messaging = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin();

var worker = builder.AddProject<Projects.Licensing_Worker>("worker")
    .WithReference(database).WaitFor(database)
    .WithReference(messaging).WaitFor(messaging);

builder.AddProject<Projects.Licensing_Api>("api")
    .WithReference(database).WaitFor(database)
    .WithReference(messaging).WaitFor(messaging)
    .WaitFor(worker)
    .WithExternalHttpEndpoints();

builder.Build().Run();