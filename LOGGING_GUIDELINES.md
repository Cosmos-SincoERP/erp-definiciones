# Guías de Logging Estructurado

Este documento proporciona guías y ejemplos para implementar logging estructurado en handlers de comandos, consultas y eventos.

## Principios Generales

### Niveles de Log

- **Trace**: Debugging muy detallado (generalmente deshabilitado)
- **Debug**: Información de debugging para desarrollo
- **Information**: Flujo normal de la aplicación
- **Warning**: Situaciones anormales pero recuperables
- **Error**: Errores que requieren atención
- **Critical**: Fallas que requieren acción inmediata

### ¿Qué Loggear?

#### ✅ SIEMPRE Loggear:
- Inicio/fin de comandos y consultas importantes
- Decisiones de negocio significativas
- Errores y excepciones
- Cambios de estado críticos
- Operaciones externas (APIs, DB, message bus)
- Validaciones de negocio fallidas

#### ❌ NUNCA Loggear:
- Contraseñas o secretos
- Datos sensibles (PII - Personal Identifiable Information)
- Información de tarjetas de crédito
- Tokens de autenticación

## Structured Logging vs String Interpolation

```csharp
// ✅ CORRECTO - Structured logging
logger.LogInformation(
    "Pedido {PedidoId} creado para cliente {ClienteId} con total {Total:C}",
    pedidoId,
    clienteId,
    total
);

// ❌ INCORRECTO - String interpolation
logger.LogInformation($"Pedido {pedidoId} creado para cliente {clienteId}");
```

**Razón**: El structured logging permite:
- Búsquedas eficientes por campos específicos
- Análisis y agregaciones en herramientas de observabilidad
- Correlación de logs relacionados

## Ejemplos de Implementación

### 1. Command Handler con Logging

```csharp
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Cosmos.DatosReferencia.Dominio.Comandos;

public class CrearProductoHandler
{
    private readonly ILogger<CrearProductoHandler> _logger;
    private readonly IProductoRepository _repository;

    public CrearProductoHandler(
        ILogger<CrearProductoHandler> logger,
        IProductoRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    public async Task<Result> Handle(CrearProducto command)
    {
        // Log del inicio del comando con parámetros relevantes
        _logger.LogInformation(
            "Iniciando creación de producto {ProductoId} con nombre {Nombre}",
            command.ProductoId,
            command.Nombre
        );

        // Agregar tags al trace de OpenTelemetry
        Activity.Current?.SetTag("command.type", nameof(CrearProducto));
        Activity.Current?.SetTag("producto.id", command.ProductoId);

        try
        {
            // Validación de negocio
            if (await _repository.ExisteProducto(command.ProductoId))
            {
                _logger.LogWarning(
                    "Intento de crear producto {ProductoId} que ya existe",
                    command.ProductoId
                );
                return Result.Failure("El producto ya existe");
            }

            // Lógica de negocio
            var producto = Producto.Crear(command);
            await _repository.Guardar(producto);

            // Log del éxito con contexto
            _logger.LogInformation(
                "Producto {ProductoId} creado exitosamente. Categoría: {Categoria}, Precio: {Precio:C}",
                command.ProductoId,
                producto.Categoria,
                producto.Precio
            );

            return Result.Success();
        }
        catch (DomainException ex)
        {
            // Errores de negocio (no usar LogError, son esperados)
            _logger.LogWarning(
                ex,
                "Error de validación al crear producto {ProductoId}: {ErrorMessage}",
                command.ProductoId,
                ex.Message
            );
            throw;
        }
        catch (Exception ex)
        {
            // Errores inesperados del sistema
            _logger.LogError(
                ex,
                "Error inesperado al crear producto {ProductoId}",
                command.ProductoId
            );
            throw;
        }
    }
}
```

### 2. Query Handler con Logging

```csharp
using Microsoft.Extensions.Logging;

namespace Cosmos.DatosReferencia.Consultas.Queries;

public class ObtenerProductoPorIdHandler
{
    private readonly ILogger<ObtenerProductoPorIdHandler> _logger;
    private readonly IQuerySession _session;

    public ObtenerProductoPorIdHandler(
        ILogger<ObtenerProductoPorIdHandler> logger,
        IQuerySession session)
    {
        _logger = logger;
        _session = session;
    }

    public async Task<ProductoDto?> Handle(ObtenerProductoPorId query)
    {
        _logger.LogDebug(
            "Consultando producto {ProductoId}",
            query.ProductoId
        );

        var producto = await _session.LoadAsync<ProductoDto>(query.ProductoId);

        if (producto == null)
        {
            _logger.LogInformation(
                "Producto {ProductoId} no encontrado",
                query.ProductoId
            );
            return null;
        }

        _logger.LogDebug(
            "Producto {ProductoId} encontrado. Nombre: {Nombre}",
            query.ProductoId,
            producto.Nombre
        );

        return producto;
    }
}
```

### 3. Event Handler con Logging

```csharp
using Microsoft.Extensions.Logging;

namespace Cosmos.DatosReferencia.Dominio.Eventos;

public class ProductoCreadoHandler
{
    private readonly ILogger<ProductoCreadoHandler> _logger;
    private readonly INotificationService _notificationService;

    public ProductoCreadoHandler(
        ILogger<ProductoCreadoHandler> logger,
        INotificationService notificationService)
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task Handle(ProductoCreado evento)
    {
        _logger.LogInformation(
            "Procesando evento ProductoCreado para producto {ProductoId}",
            evento.ProductoId
        );

        try
        {
            await _notificationService.NotificarNuevoProducto(evento);

            _logger.LogInformation(
                "Notificación enviada exitosamente para producto {ProductoId}",
                evento.ProductoId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error al enviar notificación para producto {ProductoId}",
                evento.ProductoId
            );
            // Dependiendo del caso, podríamos re-throw o manejar el error
            throw;
        }
    }
}
```

### 4. Carter Endpoint con Logging

```csharp
using Carter;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Cosmos.DatosReferencia.Comandos.API.Endpoints;

public class ProductosEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/productos", async (
            CrearProducto command,
            ICommandRouter router,
            ILogger<ProductosEndpoints> logger,
            HttpContext context) =>
        {
            var userId = context.User.Identity?.Name ?? "anonymous";

            logger.LogInformation(
                "Usuario {UserId} solicitó crear producto {ProductoId}",
                userId,
                command.ProductoId
            );

            try
            {
                await router.RouteAsync(command);

                logger.LogInformation(
                    "Comando CrearProducto ejecutado exitosamente para producto {ProductoId}",
                    command.ProductoId
                );

                return Results.Created($"/api/productos/{command.ProductoId}", command);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Error al procesar comando CrearProducto para producto {ProductoId}",
                    command.ProductoId
                );
                throw;
            }
        })
        .WithName("CrearProducto")
        .WithTags("Productos");
    }
}
```

### 5. Usando Logging Scopes

Los scopes permiten agregar contexto que se propaga a todos los logs dentro del scope:

```csharp
public async Task<Result> Handle(ProcesarPedido command)
{
    // Todos los logs dentro de este scope tendrán PedidoId y ClienteId
    using (_logger.BeginScope(new Dictionary<string, object>
    {
        ["PedidoId"] = command.PedidoId,
        ["ClienteId"] = command.ClienteId,
        ["CorrelationId"] = command.CorrelationId
    }))
    {
        _logger.LogInformation("Iniciando procesamiento de pedido");

        await ValidarInventario(command);
        _logger.LogInformation("Inventario validado");

        await ProcesarPago(command);
        _logger.LogInformation("Pago procesado");

        await EnviarNotificacion(command);
        _logger.LogInformation("Notificación enviada");

        _logger.LogInformation("Pedido procesado exitosamente");

        return Result.Success();
    }
}
```

### 6. Agregando Tags a OpenTelemetry Traces

```csharp
using System.Diagnostics;

public async Task Handle(CrearProducto command)
{
    // Agregar información adicional al trace actual
    Activity.Current?.SetTag("command.type", nameof(CrearProducto));
    Activity.Current?.SetTag("producto.id", command.ProductoId);
    Activity.Current?.SetTag("producto.categoria", command.Categoria);
    Activity.Current?.SetTag("user.id", userId);

    // Para propagar contexto entre servicios
    Baggage.SetBaggage("tenant.id", tenantId.ToString());
    Baggage.SetBaggage("correlation.id", correlationId);

    // Agregar evento personalizado al trace
    Activity.Current?.AddEvent(new ActivityEvent("Validación de negocio iniciada"));

    // Tu lógica aquí...

    Activity.Current?.AddEvent(new ActivityEvent("Validación de negocio completada"));
}
```

## Correlación de Logs

Todos los logs están automáticamente correlacionados con OpenTelemetry traces gracias a la **funcionalidad nativa de Serilog 3.1.0+**.

### Cómo funciona

Desde **Serilog 3.1.0** (septiembre 2023), Serilog captura automáticamente `TraceId` y `SpanId` desde `System.Diagnostics.Activity.Current`. No se requiere ningún enricher adicional.

Cada log incluirá automáticamente:
- **TraceId**: Identificador único de la transacción completa (desde `Activity.Current.TraceId`)
- **SpanId**: Identificador de la operación específica (desde `Activity.Current.SpanId`)

Esto permite:
- Ver todos los logs de una misma transacción
- Seguir el flujo de una request a través de múltiples servicios
- Correlacionar logs con traces en Aspire Dashboard

### Verificación

Para verificar que la correlación funciona correctamente, revisa tus logs. Deberías ver propiedades como:

```json
{
  "TraceId": "4bf92f3577b34da6a3ce929d0e0e4736",
  "SpanId": "00f067aa0ba902b7",
  "Message": "Procesando comando..."
}
```

## Mejores Prácticas

### 1. No loggear en loops intensivos

```csharp
// ❌ MAL - Log en cada iteración
foreach (var item in items)
{
    _logger.LogInformation("Procesando item {ItemId}", item.Id);
    await ProcessItem(item);
}

// ✅ BIEN - Log antes y después
_logger.LogInformation("Procesando {Count} items", items.Count);
foreach (var item in items)
{
    await ProcessItem(item);
}
_logger.LogInformation("Procesamiento completado. Items procesados: {Count}", items.Count);
```

### 2. Usar log levels apropiados

```csharp
// ✅ Information - Flujo normal
_logger.LogInformation("Usuario {UserId} inició sesión", userId);

// ✅ Warning - Situación anormal pero recuperable
_logger.LogWarning("Cache miss para producto {ProductoId}", productoId);

// ✅ Error - Error que requiere atención
_logger.LogError(ex, "Error al conectar con servicio externo");

// ✅ Critical - Falla crítica del sistema
_logger.LogCritical(ex, "Base de datos inaccesible");
```

### 3. Incluir contexto relevante

```csharp
// ❌ MAL - Falta contexto
_logger.LogError(ex, "Error al procesar");

// ✅ BIEN - Contexto completo
_logger.LogError(
    ex,
    "Error al procesar pedido {PedidoId} del cliente {ClienteId}. Estado actual: {Estado}",
    pedidoId,
    clienteId,
    estadoActual
);
```

### 4. No duplicar información que ya está en el trace

```csharp
// ❌ Redundante - OpenTelemetry ya captura esto
_logger.LogInformation("Request HTTP POST a /api/productos");

// ✅ BIEN - Información de negocio adicional
_logger.LogInformation(
    "Creando producto en categoría {Categoria} con precio {Precio:C}",
    categoria,
    precio
);
```

## Visualización en Aspire Dashboard

Los logs estarán disponibles en:
- **URL**: http://localhost:18888 (o el puerto configurado)
- **Sección Logs**: Ver todos los logs estructurados
- **Sección Traces**: Ver distributed traces con logs correlacionados

### Búsquedas útiles en Aspire:

1. **Por TraceId**: Todos los logs de una transacción
2. **Por campo estructurado**: `PedidoId = "12345"`
3. **Por nivel**: Solo errores o warnings
4. **Por aplicación**: Filtrar por "Application" property

## Configuración de Niveles por Ambiente

### Development
- Nivel: **Debug** o **Information**
- Propósito: Debugging detallado

### Production
- Nivel: **Information** o **Warning**
- Propósito: Solo información relevante para reducir costos y ruido

Configuración en `appsettings.{Environment}.json`:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning",
        "Cosmos.DatosReferencia": "Debug"
      }
    }
  }
}
```

## Resumen de Características Implementadas

✅ **Serilog 9.0** con structured logging
✅ **OpenTelemetry 1.14** para distributed tracing
✅ **Correlación automática** logs ↔ traces (nativa desde Serilog 3.1.0+)
✅ **Enriquecimiento** con Machine, Thread, Process, Environment
✅ **Middleware global** de exception handling
✅ **Health checks** avanzados (liveness/readiness)
✅ **Aspire Dashboard** para visualización
✅ **Sampling configurable** por ambiente
✅ **Filtros** para no trazar health checks

## Próximos Pasos Recomendados

1. Implementar logging en todos los handlers existentes
2. Agregar métricas personalizadas de negocio (contadores, histogramas)
3. Configurar sink adicional para producción (Application Insights, Seq)
4. Implementar request/response logging middleware si es necesario
5. Crear dashboards personalizados en herramientas de observabilidad
