using System.Reflection;
using Asp.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.Tenancy;

namespace Platform.Web;

public static class ClpWebExtensions
{
    public static WebApplicationBuilder AddClpWeb(this WebApplicationBuilder builder, Assembly contractAssembly, string contractResourceName)
    {
        builder.Services.AddSingleton(new ContractDocument(contractAssembly, contractResourceName));
#pragma warning disable AV0021
        builder.Services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        });
#pragma warning restore AV0021
        builder.Services.AddHealthChecks();
        return builder;
    }

    public static WebApplication UseClpPipeline(this WebApplication app)
    {
        app.UseExceptionHandler(handler => handler.Run(async context =>
        {
            var feature = context.Features.Get<IExceptionHandlerFeature>();
            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Clp.Unhandled");
            logger.LogError(feature?.Error, "Unhandled exception");
            await ProblemWriter.WriteAsync(context, StatusCodes.Status500InternalServerError, "internal-error", "Internal error");
        }));
        app.UseMiddleware<CorrelationMiddleware>();
        app.UseTenancy();
        app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.yaml", "v1"));

        var contract = app.Services.GetRequiredService<ContractDocument>();
        app.MapGet("/openapi/v1.yaml", () => Results.Text(contract.Yaml, "application/yaml"));
        app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = _ => false
        });
        app.MapHealthChecks("/health/ready");
        return app;
    }
}

public sealed class ContractDocument
{
    public ContractDocument(Assembly assembly, string resourceName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded contract '{resourceName}' was not found.");
        using var reader = new StreamReader(stream);
        Yaml = reader.ReadToEnd();
    }

    public string Yaml { get; }
}

public sealed class CorrelationMiddleware(RequestDelegate next, ILogger<CorrelationMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var supplied) && !string.IsNullOrWhiteSpace(supplied)
            ? supplied.ToString()
            : Guid.NewGuid().ToString("D");
        context.Response.Headers[HeaderName] = correlationId;
        using (logger.BeginScope(new Dictionary<string, object> { ["correlationId"] = correlationId }))
        {
            await next(context);
        }
    }
}
