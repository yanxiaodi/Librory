var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();
var db = postgres.AddDatabase("LibroryDb");

var api = builder.AddProject("api", "../Librory.Api/Librory.Api.csproj")
    .WithExternalHttpEndpoints()
    .WithReference(db)
    .WaitFor(db);

api.WithEndpointProxySupport(false);

builder.AddNpmApp("web", "../Librory.Web")
    .WithReference(api)
    .WithEnvironment("LIBRORY_API_URL", api.GetEndpoint("http"))
    .WithHttpEndpoint(port: 5180, env: "VITE_PORT")
    .WithExternalHttpEndpoints()
    .WaitFor(api);

builder.Build().Run();
