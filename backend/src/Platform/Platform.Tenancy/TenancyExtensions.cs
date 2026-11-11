using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Platform.Tenancy;

public static class TenancyExtensions
{
    public static WebApplicationBuilder AddTenancy(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<TenantContext>();
        builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
        var tenants = builder.Configuration.GetSection("Tenants:Allowed").Get<string[]>() ?? [];
        builder.Services.AddSingleton(new TenantCatalog(tenants));
        return builder;
    }

    public static WebApplication UseTenancy(this WebApplication app)
    {
        app.UseMiddleware<TenantMiddleware>();
        return app;
    }
}

public sealed class TenantCatalog(IReadOnlyCollection<string> allowed)
{
    public IReadOnlyCollection<string> Allowed { get; } = allowed;
}

public sealed class TenantMiddleware(RequestDelegate next, TenantCatalog catalog)
{
    public const string HeaderName = "X-Tenant-Id";

    public async Task InvokeAsync(HttpContext context, TenantContext tenant, ILogger<TenantMiddleware> logger)
    {
        if (IsExempt(context.Request.Path))
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(HeaderName, out var value) || string.IsNullOrWhiteSpace(value))
        {
            await ProblemWriter.WriteAsync(context, StatusCodes.Status400BadRequest, "tenant-required", "Tenant is required");
            return;
        }

        var tenantId = value.ToString();
        if (!catalog.Allowed.Contains(tenantId, StringComparer.Ordinal))
        {
            await ProblemWriter.WriteAsync(context, StatusCodes.Status403Forbidden, "tenant-unknown", "Tenant is unknown");
            return;
        }

        tenant.Set(tenantId);
        using (logger.BeginScope(new Dictionary<string, object> { ["tenantId"] = tenantId }))
        {
            await next(context);
        }
    }

    public static bool IsExempt(PathString path) =>
        path.StartsWithSegments("/health")
        || path.StartsWithSegments("/openapi")
        || path.StartsWithSegments("/swagger");
}

public static class ProblemWriter
{
    public static async Task WriteAsync(HttpContext context, int status, string code, string title)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        var body = new Dictionary<string, object?>
        {
            ["type"] = $"https://clp.example/problems/{code}",
            ["title"] = title,
            ["status"] = status,
            ["code"] = code,
            ["traceId"] = System.Diagnostics.Activity.Current?.Id ?? context.TraceIdentifier
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(body));
    }
}
