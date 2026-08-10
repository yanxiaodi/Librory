using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Librory.ServiceDefaults;

public static class Extensions
{
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSerilog((services, lc) =>
        {
            lc
                .MinimumLevel.Information()
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", builder.Environment.ApplicationName)
                .WriteTo.Console();

            var seqUrl = builder.Configuration.GetConnectionString("seq");
            if (!string.IsNullOrEmpty(seqUrl))
            {
                lc.WriteTo.Seq(seqUrl);
            }

        });

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        app.UseSerilogRequestLogging();
        app.MapGet("/health", () => new { status = "ok" });
        return app;
    }
}
