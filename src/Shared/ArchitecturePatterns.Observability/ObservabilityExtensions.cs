using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ArchitecturePatterns.Observability;

public static class ObservabilityExtensions
{
    public static readonly ActivitySource ActivitySource = new("ArchitecturePatterns");
    public static readonly Meter Meter = new("ArchitecturePatterns", "1.0.0");

    public static IServiceCollection AddArchitectureObservability(this IServiceCollection services)
    {
        services.AddHealthChecks().AddCheck("ready", () => HealthCheckResult.Healthy("ready"));
        services.AddSingleton(ActivitySource);
        services.AddSingleton(Meter);
        return services;
    }

    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            const string header = "X-Correlation-Id";
            var correlationId = context.Request.Headers.TryGetValue(header, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value.ToString()
                : Guid.NewGuid().ToString("N");
            context.Response.Headers[header] = correlationId;
            using var activity = ActivitySource.StartActivity("http.request");
            activity?.SetTag("correlation.id", correlationId);
            await next();
        });
}
