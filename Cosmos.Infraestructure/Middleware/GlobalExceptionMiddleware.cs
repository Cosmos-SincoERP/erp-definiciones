using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Cosmos.Infraestructure.Middleware;

/// <summary>
/// Middleware global para capturar y loggear excepciones no manejadas.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

            _logger.LogError(
                ex,
                "Excepción no manejada en {Method} {Path}. TraceId: {TraceId}",
                context.Request.Method,
                context.Request.Path,
                traceId
            );

            // Si la respuesta ya fue enviada, no podemos modificarla
            if (context.Response.HasStarted)
            {
                _logger.LogWarning("La respuesta ya fue enviada, no se puede enviar error personalizado");
                throw;
            }

            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new
            {
                error = "Error interno del servidor",
                traceId,
                timestamp = DateTime.UtcNow
            });
        }
    }
}
