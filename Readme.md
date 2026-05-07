# Solution template: Cosmos EventSourcing

## Descripción

Nuget de Plantilla de solución para event sourcing y event driven architecture.

Construye los siguientes proyectos:

- **Contabilidad.Dominio**: Contiene los agregados, comandos (con handlers integrados), eventos y excepciones.
- **Contabilidad.Consultas**: Contiene las proyecciones, consultas y queryHandlers. Los modelos de consulta debería
  estar en dominio.
- **Contabilidad.Comandos.API**: API REST para los comandos de la aplicación con minimal API, healthchecks y Open
  API.
- **Contabilidad.Consultas.API**: API REST para los comandos de la aplicación con minimal API, healthchecks y Open
  API.
- **Contabilidad.Dominio.Tests**: Abstracción del StoreEvent para pruebas y CommandHandlerTest.
- **Contabilidad.Consultas.Tests**: Pruebas conectadas para las proyecciones con un demonio de Marten y postgres
  TestContainers.
- **Contabilidad.AcceptanceTests**: Proyecto de pruebas de integración que ejecutan la API de Comandos o consultas y
  una base de datos con TestContainers.

## Correr la aplicación

### Requisitos previos

Este proyecto tiene la configuración de principalmente dos API (Comandos y Consultas), los comandos están conectados a
RabbitMQ con el fin de recibir o enviar mensajes a través de un bus de mensajes por medio de los patrones Outbox e
Inbox, por lo cual es necesario tener corriendo RabbitMQ en el puerto 5672en docker. El comando sugerido para levantar el contenedor es:

```bash
docker run -it --rm --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:4-management
```

En su navegador puede acceder a la consola de administración de RabbitMQ en `http://localhost:15672` con las credenciales por defecto:

Usuario: `guest`

Contraseña: `guest`

### Cluster de desarrollo

Para correr la aplicación primero debemos configurar el .env
en el directorio raíz de la solución, ejecute el siguiente comando:

```bash
touch .env
```

En el archivo `.env` debe definir los siguientes valores:

```env
COSMOS_FEED_PAT={el valor del feed PAT de tu cuenta de Cosmos}
```

*Es importante tener el cuenta que el PAT debe estar vigente, esto puede ocasionar errores al momento de correr la
aplicación si no es así.*

*No olvides agregar el archivo `.env` a tu `.gitignore` para evitar subirlo al repositorio.*

Ahora docker desktop debe estar instalado y ejecutándose.

```bash
docker-compose up -d --build
```

### API

Hay dos API una para comandos y una para consusltas.

La API de Comandos está expuesta en `http://localhost:8080`

La API de Consultas está expuesta en `http://localhost:8090`

### Acceder a la base de datos

Para consultar la base de datos, puede conectarse desde el IDE a un proveedor de Postgres.
Como se ve en el docker-compose, la base de datos está expuesta en el puerto 5432.

Datos de conexión:

POSTGRES_USER: ContabilidadUser

POSTGRES_PASSWORD: ContabilidadPassword

POSTGRES_DB: contabilidaddb

POSTGRES_PORT: 5432

### Observabilidad con .NET Aspire Dashboard

La solución incluye un stack completo de observabilidad con **Serilog**, **OpenTelemetry** y **.NET Aspire Dashboard**.

**Acceso al Dashboard:** `http://localhost:18888`

#### Características disponibles:

- **Logs estructurados**: Visualiza todos los logs de la aplicación con búsqueda avanzada
- **Distributed Tracing**: Sigue el flujo de requests a través de los servicios
- **Métricas**: Monitorea performance y uso de recursos
- **Correlación automática**: Logs y traces correlacionados por TraceId

#### Qué puedes ver:

- **Logs en tiempo real** de ambas APIs (Comandos y Consultas)
- **Traces distribuidos** de operaciones end-to-end
- **Métricas de runtime** (.NET GC, ThreadPool, Exceptions)
- **HTTP requests/responses** con duración y códigos de estado
- **Operaciones de Marten** (Event Store) y **Wolverine** (Message Bus)

Para más detalles sobre cómo implementar logging estructurado en tu código, consulta [LOGGING_GUIDELINES.md](./LOGGING_GUIDELINES.md).

## Tecnologías utilizadas

- **.NET 10**
- **Marten**: Event Store basado en PostgreSQL.
- **Wolverine**: Bus de mensajes para manejar comandos y eventos.
- **OpenAPI**: Documentación interactiva de la API.
- **Health Checks**: Supervisión de la salud de la aplicación.
- **Serilog**: Logging estructurado con soporte para OpenTelemetry.
- **OpenTelemetry**: Distributed tracing y métricas cloud-native.
- **.NET Aspire Dashboard**: Plataforma de observabilidad para desarrollo local.

## Características principales

- **Event Sourcing**: Uso de **Marten** para almacenar eventos, rehidratar los agregados y crear proyecciones.
- **Enrutamiento de comandos**: Mediador que invoca los handlers dependiendo del comando o evento recibido.
- La implementación  `WolverineCommandRouter` es una abstracción de Wolverine como mediador.
- **Transacciones automáticas**: `SaveChanges()` automático al finalizar la ejecución del handler.
- **Event Driven Architecture**: Implementación de patrones como Outbox e Inbox para manejar la comunicación entre servicios.

## Observabilidad y Logging

La solución implementa un stack completo de observabilidad moderna siguiendo las mejores prácticas de cloud-native applications.

### Stack implementado

- **Serilog 9.0**: Logging estructurado con múltiples enrichers
- **OpenTelemetry 1.14**: Distributed tracing y métricas
- **.NET Aspire Dashboard**: Visualización centralizada
- **Health Checks**: Endpoints de liveness y readiness

### Características principales

#### 🔍 Logging estructurado
- Todos los logs incluyen contexto rico (Machine, Thread, Process, Environment)
- Correlación automática logs ↔ traces mediante TraceId/SpanId
- Configuración flexible por ambiente (Development/Production)
- Console output y OpenTelemetry sink

#### 📊 Distributed Tracing
- Instrumentación automática de HTTP requests (incoming/outgoing)
- Traces de operaciones de Marten (Event Store)
- Traces de mensajes de Wolverine (Message Bus)
- Sampling configurable: 100% en desarrollo, 10% en producción

#### 💊 Health Checks
- `/health/live` - Liveness probe (Kubernetes)
- `/health/ready` - Readiness probe (Kubernetes)
- `/health` - Health check general (backward compatibility)
- Verificación de PostgreSQL, RabbitMQ y self-check

#### 🚨 Exception Handling
- Middleware global de captura de excepciones
- Logging automático con contexto completo
- Respuestas JSON estructuradas con TraceId

### Guía de implementación

Para implementar logging en tus handlers, consulta la guía detallada: [LOGGING_GUIDELINES.md](./LOGGING_GUIDELINES.md)

**Ejemplo rápido:**

```csharp
public class MiCommandHandler
{
    private readonly ILogger<MiCommandHandler> _logger;

    public MiCommandHandler(ILogger<MiCommandHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(MiComando comando)
    {
        _logger.LogInformation(
            "Procesando comando {ComandoId} para {EntidadId}",
            comando.Id,
            comando.EntidadId
        );

        // Tu lógica...

        _logger.LogInformation("Comando procesado exitosamente");
    }
}
```

## Configuración

### Requisitos previos

- **.NET 10 SDK**: Asegúrate de tener instalado el SDK de .NET 10.
- **Docker desktop**: La solución incluye un docker compose para levantar los contenedores de base de datos y aplcación.

### Configuración de Logging

#### Niveles de log por ambiente

Los niveles de log se configuran en `appsettings.{Environment}.json`:

**Development:**
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft": "Information"
      }
    }
  }
}
```

**Production:**
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

#### OpenTelemetry Endpoint

El endpoint de OpenTelemetry se configura en `appsettings.json`:

```json
{
  "OpenTelemetryEndpoint": "http://localhost:18889"
}
```

Para más detalles sobre configuración avanzada, consulta [LOGGING_GUIDELINES.md](./LOGGING_GUIDELINES.md).
