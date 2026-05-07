var builder = WebApplication.CreateBuilder(args);

const string serviceName = "Cosmos.DatosReferencia.Consultas.MCP.Server";

builder.Logging.AddConsole(consoleLogOptions =>
{
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithResourcesFromAssembly()
    .WithToolsFromAssembly();
    
var app = builder.Build();

app.Run();