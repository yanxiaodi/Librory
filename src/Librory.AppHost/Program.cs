var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject("api", "../Librory.Api/Librory.Api.csproj")
    .WithEndpoint("http", endpoint => endpoint.Port = 5172);

builder.AddNpmApp("web", "../Librory.Web")
    .WithReference(api)
    .WithHttpEndpoint(port: 5174, env: "VITE_PORT")
    .WithExternalHttpEndpoints()
    .WaitFor(api);

builder.Build().Run();
