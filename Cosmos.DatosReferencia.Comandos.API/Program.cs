using System.Reflection;
using Cosmos.DatosReferencia.Comandos.API;
using Cosmos.DatosReferencia.Contratos;
using Cosmos.DatosReferencia.Dominio;
using Cosmos.DatosReferencia.Dominio.Store.Extensions;
using Carter;
using Cosmos.EventDriven.CritterStack;
using Cosmos.EventDriven.CritterStack.RabbitMQ;
using Cosmos.EventSourcing.CritterStack;
using Cosmos.EventSourcing.CritterStack.Commands;
using Cosmos.Infraestructure;

const string serviceName = "Cosmos.DatosReferencia.Domain.API";

var builder = WebApplication.CreateBuilder(args);

var martenConnectionString = builder.Configuration.GetConnectionString("MartenEventStore") ??
                             throw new ArgumentNullException(
                                 $"La cadena de conexión 'MartenEventStore' no está configurada.");
var rabbitMqConnectionString = builder.Configuration.GetConnectionString("RabbitMQ");
var openTelemetryEndpoint = builder.Configuration.GetValue<string>("OpenTelemetryEndpoint") ??
                            throw new ArgumentNullException(
                                $"La url de OpenTelemtry no está configurada.");
var isProduction = builder.Environment.IsProduction();

builder.Host.UsarSerilog(serviceName, openTelemetryEndpoint);
builder.Host.UsarWolverineParaComandos(
    typeof(IDominioAssemblyMarker).Assembly,
    martenConnectionString,
    "cosmos.datosreferencia",
    builder.Environment.IsDevelopment(),
    options =>
    {
        options.HabilitarRabbitMq("RabbitMQ");
        options.HabilitarOutbox(serviceName, typeof(IContratosAssemblyMarker).Assembly);
        options.HabilitarInbox();
        // Ejemplo de como suscribirse a un exchange de RabbitMQ de un servicio productor de eventos EDA.
        // options.SuscribirseAServicio("NOMBRE_DEL_SERVICIO_PRODUCTOR", "Consumer1-From-ProducerPublisherExchange");
    }
);

builder.Services.AddOpenApi();
builder.Services.AddCarter(new DependencyContextAssemblyCatalog(Assembly.GetExecutingAssembly()));

// Aca se registran las proyecciones en línea utiles para validaciones en el dominio (usar con responsabilidad)
builder.Services.AgregarProyeccionesEnLinea();
builder.Services.AgregarOpenTelemetry(serviceName, openTelemetryEndpoint, isProduction);
builder.Services.AgregarHealthChecks(martenConnectionString, rabbitMqConnectionString);
builder.Services.AgregarMartenEventStore();
builder.Services.AgregarWolverineCommandRouter();
builder.Services.AgregarWolverineEventSender();
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