using Librory.Application;
using Librory.Infrastructure;
using Librory.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddLibroryApplication();

var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Connection string 'Postgres' not found.");

builder.Services.AddLibroryInfrastructure(connectionString);

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    name = "Librory API",
    status = "running",
    version = "0.1",
}));

app.MapDefaultEndpoints();

app.Run();
