using System.Reflection;
using Cosmos.Infraestructure.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace Cosmos.Infraestructure;

public static class HostExtensiones
{
    /// <summary>
    /// Registra el middleware global de manejo de excepciones.
    /// </summary>
    public static IApplicationBuilder UsarGlobalExceptionMiddleware(this IApplicationBuilder app) =>
        app.UseMiddleware<GlobalExceptionMiddleware>();
    
    public static void UsarSerilog(this IHostBuilder builder, string serviceName, string openTelemetryEndpoint)
    {
        builder.UseSerilog((context, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .Enrich.WithProcessId()
                .Enrich.WithEnvironmentName()
                .Enrich.WithProperty("Application", serviceName)
                .WriteTo.Console()
                .WriteTo.OpenTelemetry(options =>
                {
                    options.Endpoint = openTelemetryEndpoint;
                    options.ResourceAttributes.Add("service.name", serviceName);
                });
        });
    }

    extension(IServiceCollection services)
    {
        public void AgregarOpenTelemetry(string serviceName,
            string openTelemetryEndpoint, bool isProduction = false)
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "unknown";

            var resourceBuilder = ResourceBuilder.CreateDefault()
                .AddService(serviceName)
                .AddTelemetrySdk()
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = environment,
                    ["service.version"] = version
                });

            services.AddOpenTelemetry()
                .WithTracing(tracerProviderBuilder =>
                {
                    tracerProviderBuilder
                        // Configurar sampling: 10% en producción, 100% en desarrollo
                        .SetSampler(isProduction
                            ? new TraceIdRatioBasedSampler(0.1)
                            : new AlwaysOnSampler())
                        .AddAspNetCoreInstrumentation(options =>
                        {
                            // No trazar health checks
                            options.Filter = httpContext =>
                                !httpContext.Request.Path.StartsWithSegments("/health");

                            // Registrar excepciones en traces
                            options.RecordException = true;
                        })
                        .AddHttpClientInstrumentation(options => { options.RecordException = true; })
                        .AddSource("Marten")
                        .AddSource("Wolverine")
                        .AddSource($"{serviceName}.Business") // Para métricas custom de negocio
                        .SetResourceBuilder(resourceBuilder)
                        .AddOtlpExporter(options => { options.Endpoint = new Uri(openTelemetryEndpoint); });
                })
                .WithMetrics(metricProviderBuilder =>
                {
                    metricProviderBuilder
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation()
                        .AddMeter("Marten")
                        .AddMeter("Wolverine")
                        .AddMeter($"{serviceName}.Business") // Para métricas custom de negocio
                        .SetResourceBuilder(resourceBuilder)
                        .AddOtlpExporter(options => { options.Endpoint = new Uri(openTelemetryEndpoint); });
                });
        }

        public IHealthChecksBuilder AgregarHealthChecks(string martenConnectionString,
            string? rabbitMqConnectionString = null)
        {
            var healthChecksBuilder = services
                .AddHealthChecks()
                .AddNpgSql(martenConnectionString, name: "postgresql", tags: ["db", "ready"])
                .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(),
                    tags: ["live"]);

            if (!string.IsNullOrWhiteSpace(rabbitMqConnectionString))
            {
                healthChecksBuilder.AddRabbitMQ(
                    sp =>
                    {
                        var factory = new RabbitMQ.Client.ConnectionFactory { Uri = new Uri(rabbitMqConnectionString) };
                        return factory.CreateConnectionAsync();
                    },
                    name: "rabbitmq",
                    tags: ["messaging", "ready"]
                );
            }

            return healthChecksBuilder;
        }
    }
}