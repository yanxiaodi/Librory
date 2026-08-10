var builder = DistributedApplication.CreateBuilder(args);

var seq = builder.AddContainer("seq", "datalust/seq:latest")
    .WithEnvironment("ACCEPT_EULA", "Y")
    .WithEnvironment("SEQ_FIRSTRUN_NOAUTHENTICATION", "true")
    .WithHttpEndpoint(port: 5341, targetPort: 80)
    .WithExternalHttpEndpoints();

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();
var db = postgres.AddDatabase("LibroryDb");

var api = builder.AddProject("api", "../Librory.Api/Librory.Api.csproj")
    .WithExternalHttpEndpoints()
    .WithEnvironment("ConnectionStrings__seq", seq.GetEndpoint("http"))
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
