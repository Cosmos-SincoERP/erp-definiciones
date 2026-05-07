var builder = WebApplication.CreateBuilder(args);

const string serviceName = "Cosmos.DatosReferencia.Comandos.MCP.Server";

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