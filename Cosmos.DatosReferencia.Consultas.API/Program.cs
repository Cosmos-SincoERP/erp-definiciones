using System.Reflection;
using Cosmos.DatosReferencia.Consultas;
using Cosmos.DatosReferencia.Consultas.API;
using Carter;
using Cosmos.DatosReferencia.Dominio.Store.Extensions;
using Cosmos.EventSourcing.CritterStack;
using Cosmos.EventSourcing.CritterStack.Queries;
using Cosmos.Infraestructure;

const string serviceName = "Cosmos.DatosReferencia.Queries.API";

var builder = WebApplication.CreateBuilder(args);

var martenConnectionString = builder.Configuration.GetConnectionString("MartenEventStore") ??
                             throw new ArgumentNullException(
                                 $"La cadena de conexión 'MartenEventStore' no está configurada.");
var openTelemetryEndpoint = builder.Configuration.GetValue<string>("OpenTelemetryEndpoint") ??
                            throw new ArgumentNullException(
                                $"La url de OpenTelemtry no está configurada.");
var isProduction = builder.Environment.IsProduction();

builder.Host.UsarSerilog(serviceName, openTelemetryEndpoint);
builder.Host.UsarWolverineParaConsultas(
    typeof(IConsultasAssemblyMarker).Assembly,
    martenConnectionString,
    "cosmos.datosreferencia",
    builder.Environment.IsDevelopment(),
    ProyeccionesRegister.AgregarProyecciones
);

builder.Services.AddOpenApi();
builder.Services.AddCarter(new DependencyContextAssemblyCatalog(Assembly.GetExecutingAssembly()));

builder.Services.AgregarOpenTelemetry(serviceName, openTelemetryEndpoint, isProduction);
builder.Services.AgregarHealthChecks(martenConnectionString);
builder.Services.AgregarMartenProjectionStore();
builder.Services.AgregarWolverineQueryRouter();
builder.Services.AgregarDomainStore();

var app = builder.Build();

// Middleware de manejo global de excepciones (debe ir primero)
app.UsarGlobalExceptionMiddleware();

app.MapCarter();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
    app.MapOpenApi();

// app.UseHttpsRedirection();

// Health checks endpoints
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();