using Librory.Application;
using Librory.Application.Families;
using Librory.Infrastructure;
using Librory.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddLibroryApplication();
builder.Services.AddLibroryInfrastructure();

var app = builder.Build();

app.UseMiddleware<CurrentFamilyContextMiddleware>();

app.MapGet("/", () => Results.Ok(new
{
    name = "Librory API",
    status = "running",
    version = "0.1",
}));

app.MapDefaultEndpoints();

app.Run();
